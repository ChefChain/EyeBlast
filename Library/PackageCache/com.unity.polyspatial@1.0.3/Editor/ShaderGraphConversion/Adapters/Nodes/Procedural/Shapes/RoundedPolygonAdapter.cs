using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RoundedPolygonAdapter : AbstractUVNodeAdapter<RoundedPolygonNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Rounded-Polygon-Node.html
            // (but MaterialX doesn't support fwidth, so we implement using splitlr)
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "RoundedPolygon", @"
float2 UV2 = UV * 2. + float2(-1.,-1.);
float epsilon = 1e-6;
float2 UV3 = float2(UV2.x / ( Width + (Width==0)*epsilon), UV2.y / ( Height + (Height==0)*epsilon));
float Roundness2 = clamp(Roundness, 1e-6, 1.);
float i_sides = floor( abs( Sides ) );
float fullAngle = 2. * PI / i_sides;
float halfAngle = fullAngle / 2.;
float opositeAngle = HALF_PI - halfAngle;
float diagonal = 1. / cos( halfAngle );
// Chamfer values
float chamferAngle = Roundness2 * halfAngle; // Angle taken by the chamfer
float remainingAngle = halfAngle - chamferAngle; // Angle that remains
float ratio = tan(remainingAngle) / tan(halfAngle); // This is the ratio between the length of the polygon's triangle and the distance of the chamfer center to the polygon center
// Center of the chamfer arc
float2 chamferCenter = float2(
    cos(halfAngle) ,
    sin(halfAngle)
)* ratio * diagonal;
// starting of the chamfer arc
float2 chamferOrigin = float2(
    1.,
    tan(remainingAngle)
);
// Using Al Kashi algebra, we determine:
// The distance distance of the center of the chamfer to the center of the polygon (side A)
float distA = length(chamferCenter);
// The radius of the chamfer (side B)
float distB = 1. - chamferCenter.x;
// The refence length of side C, which is the distance to the chamfer start
float distCref = length(chamferOrigin);
// This will rescale the chamfered polygon to fit the uv space
// diagonal = length(chamferCenter) + distB;
float uvScale = diagonal;
float2 UV4 = UV3 * uvScale;
float2 polaruv = float2 (
    atan2( UV4.y, UV4.x ),
    length(UV4)
);
float2 polaruv2 = float2(
    abs(fmod( polaruv.x + HALF_PI + 2*PI + halfAngle, fullAngle ) - halfAngle),
    polaruv.y);
float2 UV5 = float2( cos(polaruv2.x), sin(polaruv2.x) ) * polaruv2.y;
// Calculate the angle needed for the Al Kashi algebra
float angleRatio = 1. - (polaruv2.x-remainingAngle) / chamferAngle;
// Calculate the distance of the polygon center to the chamfer extremity
float distC = sqrt( distA*distA + distB*distB - 2.*distA*distB*cos( PI - halfAngle * angleRatio ) );
float chamferZone = ( halfAngle - polaruv2.x ) < chamferAngle;
float d = lerp( UV5.x, polaruv2.y / distC, chamferZone );
// Output this to have the shape mask instead of the distance field
Out = splitlr(1, 0, 1, float2(d, 0));");
        }
    }
}