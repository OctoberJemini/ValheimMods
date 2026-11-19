using UnityEngine;

namespace OctoberJemini.PortalAtlas
{
    internal static class MapProjection
    {
        // Rect coordinates include the RectTransform pivot. Native anchored pin
        // coordinates start at the bottom left and cannot be used as local points.
        internal static Vector2 ToLocal(float worldX,float worldZ,float metresPerPixel,int textureSize,Rect uv,Rect target)
        {
            float x=(worldX/metresPerPixel+textureSize/2)/(float)textureSize;
            float y=(worldZ/metresPerPixel+textureSize/2)/(float)textureSize;
            return new Vector2(target.xMin+(x-uv.xMin)/uv.width*target.width,
                target.yMin+(y-uv.yMin)/uv.height*target.height);
        }
    }
}
