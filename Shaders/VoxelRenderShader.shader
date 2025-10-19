
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

            bool BoxN(float3 ro, float3 rd, float3 pos, float sizeF, out float dist, out float3 normal)
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

                return hit > 0.5;
            }

            float GetDistAndN(float3 po, float3 rd, float3 pos, float s, out float3 n)
            {
                float3 b = pos + step(0.0, rd) * s;
                float3 t = (b - po) * rcp(rd);

                float dist = min(t.x, min(t.y, t.z));

                n = step(t, dist.xxx) * -sign(rd);

                return dist;
            }

            int GetICoord(float3 nodePo) 
            {
                int3 res = (int3)(abs(floor(nodePo))) & 1; 
                return res.x | (res.y << 1) | (res.z << 2);
            }

            bool VoxelTrace(float3 ro, inout float distance, float3 rd, TR tr,
                 out float3 normal, out float3 color, out float bounds) 
            {
                float3 pos = tr.pos - tr.scale * 0.5;

                bool isHit = false; 

                normal = 0.0;
                color = 0.0;
                bounds = 0.025;

                int depth = 0;
                int stack1 = 0;
                // int2 stack2 = 0;
                float3 po = ro + rd * distance;

                Node node = Nodes[tr.index];

                float nodeScale = tr.scale;
                float nodeScaleRev = 1.0 / tr.scale;

                [loop]
                for (int i = 0; i <= 200; i++) 
                {
                    float3 nodePo = (po - pos) * nodeScaleRev;
                    int nodeICoord = GetICoord(nodePo);

                    // bool a = depth < 10;
                    // int currentStack = a ? stack2.x : stack2.y;
                    // int currentDepth = a ? depth : depth - 10;
                    // int nodeICoordFromStack = (currentStack >> (currentDepth * 3)) & 7;
                    int nodeICoordFromStack = (stack1 >> (depth * 3)) & 7;

                    if (nodeICoord == nodeICoordFromStack)
                    {
                        float3 childPo = (po - pos) * nodeScaleRev * 2.0;
                        int childICoord = GetICoord(childPo);

                        if ((node.mask & (1 << childICoord)) == 0)
                        {
                            float childScale = 0.5 * nodeScale;
                            float3 childPos = floor(childPo) * childScale + pos; 

                            float dist = GetDistAndN(po, rd, childPos, childScale, normal);
                            
                            float epsilon = 1e-4 * nodeScale / log(1.0 - dot(rd, normal));
                            float distE = dist + epsilon;

                            if (distE <= 0.0) 
                            { 
                                break;
                            }

                            distance += distE;
                            po = ro + rd * distance;
                        }
                        else
                        {
                            bounds += 0.025;

                            int childDepth = depth + 1;

                            if (distance >= LODDist && childDepth == tr.depth - 1 && LODUse) 
                            {
                                isHit = true;

                                Node node0 = Nodes[tr.index + node.child + firstbitlow(node.mask)];
                                int i = firstbitlow(node0.mask);
                                int j = i >> 1;
                                int rgb = j == 0 ? node0.col0 : (j == 1 ? node0.col1 : (j == 2 ? node0.col2 : node0.col3));
                                rgb = (rgb << (15 * (1 - (i & 1)) + 2)) >> 17;
                                int r = (rgb & 0x7c00) >> 10;
                                int g = (rgb & 0x3e0) >> 5;
                                int b = rgb & 0x1f;
                                color = float3(r, g, b) * 0.0322;

                                break;
                            }

                            if (node.child == -1)
                            {
                                isHit = true;

                                int j = childICoord >> 1;
                                int rgb = j == 0 ? node.col0 : (j == 1 ? node.col1 : (j == 2 ? node.col2 : node.col3));
                                rgb = (rgb << (15 * (1 - (childICoord & 1)) + 2)) >> 17;
                                int r = (rgb & 0x7c00) >> 10;
                                int g = (rgb & 0x3e0) >> 5;
                                int b = rgb & 0x1f;
                                color = float3(r, g, b) * 0.0322;

                                break;
                            }

                            // int dChildDepth = childDepth < 10 ? childDepth : childDepth - 10;

                            // if (a) 
                            // {
                            //     stack2.x &= ~(7 << (dChildDepth * 3));
                            //     stack2.x |= (childICoord << (dChildDepth * 3));
                            // }
                            // else 
                            // {
                            //     stack2.y &= ~(7 << (dChildDepth * 3));
                            //     stack2.y |= (childICoord << (dChildDepth * 3));
                            // }

                            stack1 &= ~(7 << (childDepth * 3));
                            stack1 |= (childICoord << (childDepth * 3));

                            uint count = countbits(node.mask & ((1 << childICoord) - 1));
                            node = Nodes[tr.index + node.child + count];
                            depth += 1;
                            nodeScaleRev *= 2.0;
                            nodeScale *= 0.5;
                        }
                    }
                    else 
                    {
                        if (depth == 0) 
                        {
                            break;
                        }

                        depth -= 1;
                        nodeScaleRev *= 0.5;
                        nodeScale *= 2.0;
                        node = Nodes[tr.index + node.parent];
                    }
                }

                return isHit;
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
                float distance = 0.0;
                bool isHitB = false;
                bool isHit = false;

                float3 color = 0.0;
                float3 po = ro;

                TR trMin;

                [loop]
                for (int j = 0; j < COUNT; j++) 
                {
                    isHitB = false;
                    float minDistB = MaxRenderDist;

                    [loop]
                    for (int i = 0; i < COUNT; i++) 
                    {
                        TR tr = TRs[i];

                        float3 pos = tr.pos - tr.scale * 0.5;

                        if (all(po >= pos && po < pos + tr.scale)) 
                        {
                            minDistB = distance;
                            isHitB = true;
                            trMin = tr;

                            break;
                        }

                        float d; float3 n;
                        bool h = BoxN(ro, rd, pos, tr.scale, d, n);

                        // if (h && distance == 0.0 && d == 0.0) 
                        // {
                        //     minDistB = d;
                        //     isHitB = h;
                        //     trMin = tr;
                        //     break;
                        // }

                        if (h && d < minDistB && d > distance)
                        { 
                            minDistB = d;
                            isHitB = h;
                            trMin = tr;
                        }           
                    }

                    if (!isHitB) 
                    {
                        color = 0.0;
                        break;
                    }

                    distance = minDistB != 0.0 ? minDistB + 1e-4 * trMin.scale : 0.0;

                    float3 n; float3 c; float b;
                    bool h = VoxelTrace(ro, distance, rd, trMin, n, c, b);

                    po = ro + rd * distance;

                    if (h) 
                    {
                        color = c;
                        isHit = true;
                        break;
                    }
                }

                if (!isHit) 
                {
                    color = GetEnvironmentLight(ro, rd);
                }

                float3 result = color;

                return result;
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