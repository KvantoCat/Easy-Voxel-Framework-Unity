
Shader "VLib/Render"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct Ray 
            {
                float3 ro;
                float3 rd;
            };
             
            float4x4 LocalToWorldMatrix;
            float3 ViewParams;
            float3 CameraPos;
            float MaxRenderDist; 

            float3 SkyHorizonColor;
            float3 SkyZenithColor;
            float3 GroundColor;
            float3 SunLightDir;
            float SunFocus;
            float SunIntensity;
             
            float3 Position;
            float Scale;

            int LODUse;
            float LODDist;

            struct Node 
            {
                int mask;
                int child;
                int parent;
                int col0;
                int col1;
                int col2;
                int col3;
            };

            struct TR 
            {
                float scale;
                int index;
                int depth;
                float3 pos;
            };

            StructuredBuffer<Node> Nodes;
            StructuredBuffer<TR> TRs;

            int COUNT;

            bool BoxN(float3 ro, float3 rd, float3 pos, float sizeF,
                out float dist, out float3 normal)
            {
                float3 invRd = rcp(rd);
                float3 t0 = (pos - ro) * invRd;
                float3 t1 = (pos + sizeF - ro) * invRd;

                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);

                float tNear = max(tMin.x, max(tMin.y, tMin.z));
                float tFar  = min(tMax.x, min(tMax.y, tMax.z));

                float hit = step(0.0, tFar) * step(tNear, tFar) * step(tNear, MaxRenderDist);

                dist = max(tNear, 0.0) * hit;

                float3 nearEq = step(tMin, tNear.xxx) * step(tNear.xxx, tMin);
                normal = -sign(rd) * nearEq * hit;

                return hit >= 0.5;
            }

            float GetDistAndN(float3 po, float3 rd, float3 rdInv,
                float3 pos, float s, out float3 n)
            {
                float3 b = pos + step(0.0, rd) * s;
                float3 t = (b - po) * rdInv;

                float dist = min(t.x, min(t.y, t.z));

                n = step(t, dist.xxx) * -sign(rd);

                return dist;
            }

            int GetICoord(float3 nodePo) 
            {
                int3 res = (int3)(floor(nodePo)) & 1; 
                return res.x | (res.y << 1) | (res.z << 2);
            }

            bool VoxelTrace(float3 ro, float3 rd, TR tr, inout float distance,
                 inout float3 normal, out float3 color, out float bounds) 
            {
                const int ITERCOUNT = 500;
                const float3 POS = tr.pos - tr.scale * 0.5;
                const float3 RDINV = rcp(rd);

                bool hit = false; 
                bounds = 0.025;

                int depth = 0;
                int stack = 0;

                Node node = Nodes[tr.index];

                float3 po = ro + rd * distance;
                float nodeScale = tr.scale;
                float nodeScaleRev = 1.0 / tr.scale;

                [loop]
                for (int iter = 0; iter <= ITERCOUNT; iter++) 
                {
                    float3 nodePo = (po - POS) * nodeScaleRev;
                    int nodeICoord = GetICoord(nodePo);

                    int nodeICoordFromStack = (stack >> (depth * 3)) & 7;

                    if (nodeICoord == nodeICoordFromStack)
                    {
                        float3 childPo = nodePo * 2.0;
                        int childICoord = GetICoord(childPo);

                        if ((node.mask & (1 << childICoord)) == 0)
                        {
                            float childScale = 0.5 * nodeScale;
                            float3 childPos = floor(childPo) * childScale + POS; 

                            float dist = GetDistAndN(po, rd, RDINV, childPos, childScale, normal);
                            
                            float epsilon = 1e-4 * childScale / log(1.0 - dot(rd, normal));
                            float distE = dist + epsilon;

                            if (distE <= 0.0) break;

                            distance += distE;
                            po = ro + rd * distance;
                        }
                        else
                        {
                            if (node.child == -1) 
                            {
                                int j = childICoord >> 1;
                                int4 colors = int4(node.col0, node.col1, node.col2, node.col3);
                                int rgb = colors[j];
                                rgb = (rgb << (15 * (1 - (childICoord & 1)) + 2)) >> 17;
                                int r = (rgb & 0x7c00) >> 10;
                                int g = (rgb & 0x3e0) >> 5;
                                int b = rgb & 0x1f;
                                color = float3(r, g, b) * 0.0322;

                                hit = true;
                                break;
                            }

                            depth += 1;

                            if (distance >= LODDist && depth == tr.depth - 1 && LODUse) 
                            {
                                Node node0 = Nodes[tr.index + node.child + firstbitlow(node.mask)];
                                int i = firstbitlow(node0.mask);
                                int j = i >> 1;
                                int4 colors = int4(node0.col0, node0.col1, node0.col2, node0.col3);
                                int rgb = colors[j];
                                rgb = (rgb << (15 * (1 - (i & 1)) + 2)) >> 17;
                                int r = (rgb & 0x7c00) >> 10;
                                int g = (rgb & 0x3e0) >> 5;
                                int b = rgb & 0x1f;
                                color = float3(r, g, b) * 0.0322;

                                hit = true;
                                break;
                            }

                            nodeScaleRev *= 2.0;
                            nodeScale *= 0.5;
                            bounds += 0.025;

                            stack &= ~(7 << (depth * 3));
                            stack |= (childICoord << (depth * 3));

                            uint count = countbits(node.mask & ((1 << childICoord) - 1));
                            node = Nodes[tr.index + node.child + count];
                        }
                    }
                    else 
                    {
                        if (depth == 0) break;

                        depth -= 1;
                        nodeScaleRev *= 0.5;
                        nodeScale *= 2.0;
                        node = Nodes[tr.index + node.parent];
                    }
                }

                return hit;
            }

            float3 GetEnvironmentLight(float3 ro, float3 rd) 
            {
                float skyGradientT = pow(smoothstep(0.0, 0.4, rd.y), 0.2);
                float3 skyGradient = lerp(SkyHorizonColor, SkyZenithColor, skyGradientT);

                float sun = pow(max(0.0, dot(rd, -SunLightDir)), SunFocus) * SunIntensity;

                float resultGradientT = smoothstep(-0.02, 0.0, rd.y);
                float sunMask = resultGradientT >= 1.0;

                float3 result = lerp(GroundColor, skyGradient, resultGradientT) + sun * sunMask;

                return result;
            }

            float3 SceneTraceTEST(float3 ro, float3 rd) 
            {
                const int ITERS = 5;
                const float W = 1.5;

                float distance = 0.0;
                float3 normal = 0.0;
                float3 color = 0.0;
                bool isHit = false;
                float3 po = ro;

                [loop]
                for (int j = 0; j < ITERS; j++) 
                {
                    TR tr;
                    float distBoxMin = MaxRenderDist;
                    bool isHitBox = false;

                    [loop]
                    for (int i = 0; i < COUNT; i++) 
                    {
                        TR trI = TRs[i];
                        float3 pos = trI.pos - trI.scale * 0.5;

                        float d; float3 n;
                        bool h = BoxN(ro, rd, pos, trI.scale, d, n);

                        if (!h) continue;

                        if (all(po >= pos && po < pos + trI.scale)) 
                        { 
                            distBoxMin = distance;
                            isHitBox = true;
                            tr = trI;
                            normal = n;
                            break;
                        }

                        if (d < distBoxMin && d > distance)
                        { 
                            distBoxMin = d;
                            isHitBox = true;
                            tr = trI;
                            normal = n;
                        }           
                    }

                    if (!isHitBox) break;

                    distance = distBoxMin != 0.0 ? distBoxMin + 1e-5 * tr.scale : 0.0;

                    float bounds;
                    isHit = VoxelTrace(ro, rd, tr, distance, normal, color, bounds);

                    po = ro + rd * distance;

                    if (isHit) break;
                }

                float diff = dot(normal, -SunLightDir);
                diff = (diff + W) / (1.0 + W);
                color *= diff;

                if (!isHit) color = GetEnvironmentLight(ro, rd);

                return color;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                float3 viewPointLocal = float3(uv - 0.5, 1.0) * ViewParams;
                float3 viewPoint = mul(LocalToWorldMatrix, float4(viewPointLocal, 1.0));

                float3 ro = CameraPos;
                float3 rd = normalize(viewPoint - ro);    
                float3 col = SceneTraceTEST(ro, rd);

                return float4(col, 0.0);
            }

            ENDCG
        }
    }
}