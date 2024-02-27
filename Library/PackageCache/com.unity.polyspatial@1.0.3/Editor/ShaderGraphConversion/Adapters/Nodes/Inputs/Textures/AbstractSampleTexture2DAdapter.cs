using System.IO;

namespace UnityEditor.ShaderGraph.MaterialX
{
    abstract class AbstractSampleTexture2DAdapter<T> : AbstractUVNodeAdapter<T>
        where T : AbstractMaterialNode
    {
        protected static string GetExpr(string sampleExpr, TextureType textureType)
        {
            StringWriter writer = new();

            if (textureType == TextureType.Normal)
            {
                // Multiply by two and subtract one to transform [0, 1] to [-1, 1].
                writer.WriteLine($"float4 rescaled = {sampleExpr} * 2 - 1;");

                // Multiply rescaled by itself to get squares of all components,
                // dot the result with (0, 1, 0, 1) to get the sum of y*y and w*w (w and y being normal x and y),
                // subtract that sum from one, and take the square root to get the normal z.
                writer.WriteLine("float z = sqrt(1 - dot(rescaled * rescaled, float4(0, 1, 0, 1)));");

                // Form the reconstructed normal from w, y, and sqrt(1 - y*y - w*w).
                writer.WriteLine("float4 reconstructed = float4(rescaled.w, rescaled.y, z, 1);");

                // If alpha < 1, assume we're dealing with normals packed into GB/Alpha.
                writer.WriteLine("RGBA = (rescaled.w == 1) ? rescaled : reconstructed;");
            }
            else
            {
                writer.WriteLine($"RGBA = {sampleExpr};");
            }
            writer.WriteLine("R = RGBA.r; G = RGBA.g; B = RGBA.b; A = RGBA.a;");

            return writer.ToString();
        }
    }
}