using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftLie
{
    public static class DynamicTextureUtils
    {
        public static int GetPixelSizeInBytes(this TextureDescriptionFormat format)
        {
            const int floatSizeInBytes = 4;
            const int shortSizeInBytes = 2;
            const int unormSizeInBytes = 1;
            switch (format)
            {
                case TextureDescriptionFormat.R32G32B32A32_Float:
                    return floatSizeInBytes * 4;

                case TextureDescriptionFormat.R8G8B8A8_UNorm:
                case TextureDescriptionFormat.B8G8R8A8_UNorm:
                    return unormSizeInBytes * 4;

                case TextureDescriptionFormat.R32_Float:
                    return floatSizeInBytes * 1;

                case TextureDescriptionFormat.R8_UNorm:
                    return unormSizeInBytes * 1;

                    //no default case to get an error here when someone adds more formats
            }

            throw new NotImplementedException("Wrong texture format or texture format not implementd.");
        }
    }
}
