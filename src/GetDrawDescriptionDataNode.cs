using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;

using VVVV.DX11;
using SlimDXMatrix = SlimDX.Matrix;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using CraftLie;
using SharpDX;
using VVVV.Utils.VColor;
using VL.Lib.Collections;

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "GetDrawDescriptionData", Category = "DX11.Buffer", Version = "CraftLie", Author = "tonfilm")]
    public class GetDrawDescriptionDataNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification, IDisposable
    {
        [Import()]
        protected IPluginHost2 pluginHost;

        [Input("Layer", Order = 5)]
        protected IDiffSpread<DrawDescriptionLayer> FLayerIn;

        [Input("Keep In Memory", DefaultValue = 0, Order = 6)]
        protected ISpread<bool> FKeep;

        [Input("Preferred Buffer Type", DefaultValue = 0, Order = 6, Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11BufferUploadType> FBufferType;

        [Input("Apply", IsBang = true, DefaultValue = 1, Order = 7)]
        protected ISpread<bool> FApply;

        //geometry

        [Output("Geometry Out")]
        protected Pin<DX11Resource<IDX11Geometry>> FGeometryOutput;

        [Output("Instance Counts")]
        protected ISpread<int> FInstanceCounts;

        [Output("Transformation")]
        protected ISpread<SlimDXMatrix> FTransformation;

        [Output("Color")]
        protected ISpread<RGBAColor> FColor;

        [Output("Texture Path")]
        protected ISpread<string> FTexturePath;

        [Output("Material Index")]
        protected ISpread<int> FMaterialIndex;

        [Output("Space Index")]
        protected ISpread<int> FSpaceIndex;

        [Output("Transform Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FTransformOutput;

        [Output("Transform Counts")]
        protected ISpread<int> FTransformCounts;

        [Output("Color Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FColorOutput;

        [Output("Color Counts")]
        protected ISpread<int> FColorCounts;

        //sprites

        [Output("Sprites Geometry Out")]
        protected Pin<DX11Resource<IDX11Geometry>> FSpritesGeometryOutput;

        [Output("Sprites Transformations")]
        protected ISpread<SlimDXMatrix> FSpritesTransformations;

        [Output("Sprites Space Index")]
        protected ISpread<int> FSpritesSpaceIndex;

        [Output("Sprites Position Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FSpritesPositionOutput;

        [Output("Sprites Position Counts")]
        protected ISpread<int> FSpritesPositionCounts;

        [Output("Sprites Size Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FSpritesSizeOutput;

        [Output("Sprites Size Counts")]
        protected ISpread<int> FSpritesSizeCounts;

        [Output("Sprites Color Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FSpritesColorOutput;

        [Output("Sprites Color Counts")]
        protected ISpread<int> FSpritesColorCounts;

        [Output("Sprites Texture Path")]
        protected ISpread<string> FSpritesTexturePath;

        //text

        [Output("Texts")]
        protected ISpread<string> FTexts;

        [Output("Text Transformations")]
        protected ISpread<SlimDXMatrix> FTextTransformations;

        [Output("Text Space Index")]
        protected ISpread<int> FTextSpaceIndex;

        [Output("Text Colors")]
        protected ISpread<RGBAColor> FTextColors;

        [Output("Text Sizes")]
        protected ISpread<float> FTextSizes;

        [Output("Font Names")]
        protected ISpread<string> FTextFontNames;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        DX11BufferUploadType currentBufferType = DX11BufferUploadType.Dynamic;
        private bool FInvalidate;
        private bool FFirst = true;
        private int FLayerInSpreadMax;

        protected virtual bool NeedConvert { get { return false; } }

        DrawDescriptionLayer FMainBuffer;

        int FOldGeometryOutCount = 0;
        int FOldSpritesGeometryOutCount = 0;

        public void Evaluate(int SpreadMax)
        {
            this.FInvalidate = false;

            //buffer outputs
            if (this.FApply[0] || this.FFirst)
            {
                if (this.FLayerIn.SliceCount > 0)
                {
                    this.FTransformOutput.SliceCount = 1;
                    this.FColorOutput.SliceCount = 1;

                    this.FValid.SliceCount = 1;

                    //create geometry buffer resources
                    if (this.FTransformOutput[0] == null) { this.FTransformOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }
                    if (this.FColorOutput[0] == null) { this.FColorOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }

                    //creates sprites buffer resources
                    if (this.FSpritesPositionOutput[0] == null) { this.FSpritesPositionOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }
                    if (this.FSpritesSizeOutput[0] == null) { this.FSpritesSizeOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }
                    if (this.FSpritesColorOutput[0] == null) { this.FSpritesColorOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }

                    //multiple layer input?
                    if (FLayerIn.SliceCount > 1)
                    {
                        FMainBuffer = DrawDescriptionLayer.Unite(FLayerIn);
                    }
                    else
                    {
                        FMainBuffer = FLayerIn[0];
                    }

                    //null connected?
                    if (FMainBuffer == null)
                        FMainBuffer = DrawDescriptionLayer.Default;
                }
                else //no output
                {
                    //geos
                    this.FGeometryOutput.SafeDisposeAll();
                    this.FGeometryOutput.SliceCount = 0;

                    this.FTransformOutput.SafeDisposeAll();
                    this.FTransformOutput.SliceCount = 0;

                    this.FColorOutput.SafeDisposeAll();
                    this.FColorOutput.SliceCount = 0;

                    this.FInstanceCounts.SliceCount = 0;
                    this.FTransformation.SliceCount = 0;
                    this.FTexturePath.SliceCount = 0;
                    this.FColor.SliceCount = 0;
                    this.FMaterialIndex.SliceCount = 0;
                    this.FSpaceIndex.SliceCount = 0;
                    this.FTransformCounts.SliceCount = 0;
                    this.FColorCounts.SliceCount = 0;


                    //sprites

                    this.FSpritesPositionOutput.SafeDisposeAll();
                    this.FSpritesPositionOutput.SliceCount = 0;

                    this.FSpritesSizeOutput.SafeDisposeAll();
                    this.FSpritesSizeOutput.SliceCount = 0;

                    this.FSpritesColorOutput.SafeDisposeAll();
                    this.FSpritesColorOutput.SliceCount = 0;

                    this.FSpritesTransformations.SliceCount = 0;
                    this.FSpritesPositionCounts.SliceCount = 0;
                    this.FSpritesSizeCounts.SliceCount = 0;
                    this.FSpritesColorCounts.SliceCount = 0;
                    this.FSpritesTexturePath.SliceCount = 0;
                    this.FSpritesSpaceIndex.SliceCount = 0;

                    //text
                    this.FTextSpaceIndex.SliceCount = 0;
                    this.FTextTransformations.SliceCount = 0;
                    this.FTextColors.SliceCount = 0;
                    this.FTextSizes.SliceCount = 0;
                    this.FTexts.SliceCount = 0;
                    this.FTextFontNames.SliceCount = 0;

                    this.FValid.SliceCount = 0;
                }

                this.FLayerInSpreadMax = this.FLayerIn.SliceCount;
                this.FInvalidate = true;
                this.FFirst = false;

                //mark buffers changed
                this.FTransformOutput.Stream.IsChanged = true;
                this.FColorOutput.Stream.IsChanged = true;

                this.FSpritesPositionOutput.Stream.IsChanged = true;
                this.FSpritesSizeOutput.Stream.IsChanged = true;
                this.FSpritesColorOutput.Stream.IsChanged = true;

                //set all normal output pins and get the total counts
                UpdateNormalPins();
            }

        }

        int FTotalTransformCount;
        int FTotalColorCount;

        int FTotalSpritesPositionCount;
        int FTotalSpritesSizeCount;
        int FTotalSpritesColorCount;

        protected Matrix[] FBufferTrans = new Matrix[4096];
        protected Color4[] FBufferColor = new Color4[4096];

        protected Vector3[] FBufferSpritesPosition = new Vector3[8192];
        protected Vector2[] FBufferSpritesSize = new Vector2[8192];
        protected Color4[] FBufferSpritesColor = new Color4[8192];

        private void UpdateNormalPins()
        {
            UpdateNormalGeometryPins();
            UpdateNormalSpritesPins();
            UpdateNormalTextPins();
        }

        private void UpdateNormalGeometryPins()
        {
            var descriptions = FMainBuffer.DrawDescriptions;
            var outCount = descriptions.Count;

            FGeometryOutput.SliceCount = outCount;
            FTransformation.SliceCount = outCount;
            FTexturePath.SliceCount = outCount;
            FColor.SliceCount = outCount;
            FMaterialIndex.SliceCount = outCount;
            FSpaceIndex.SliceCount = outCount;

            FInstanceCounts.SliceCount = outCount;
            FTransformCounts.SliceCount = outCount;
            FColorCounts.SliceCount = outCount;

            FTotalTransformCount = 0;
            FTotalColorCount = 0;

            //geometry output
            if (outCount != FOldGeometryOutCount || this.FFirst)
            {
                this.FInvalidate = true;

                //Dispose old
                this.FGeometryOutput.SafeDisposeAll();

                this.FGeometryOutput.SliceCount = outCount;
                this.FOldGeometryOutCount = outCount;

                for (int i = 0; i < outCount; i++)
                {
                    this.FGeometryOutput[i] = new DX11Resource<IDX11Geometry>();
                }
            }

            for (int i = 0; i < outCount; i++)
            {
                var desc = descriptions[i];

                FInstanceCounts[i] = desc.InstanceCount;

                var transCount = desc.InstanceTransformations.Count;
                var colCount = desc.InstanceColors.Count;
                FTransformCounts[i] = transCount;
                FColorCounts[i] = colCount;

                FTotalTransformCount += transCount;
                FTotalColorCount += colCount;

                FTransformation[i] = ToSlimDXMatrix(desc.Transformation);
                FTexturePath[i] = desc.TexturePath;
                FColor[i] = ToRGBAColor(desc.Color);
                FMaterialIndex[i] = (int)desc.Shading;
                FSpaceIndex[i] = (int)desc.Space;
            }
        }

        private void UpdateNormalSpritesPins()
        {
            var descriptions = FMainBuffer.SpritesDescriptions;
            var spritesCount = descriptions.Count;

            FSpritesGeometryOutput.SliceCount = spritesCount;
            FSpritesTransformations.SliceCount = spritesCount;
            FSpritesSpaceIndex.SliceCount = spritesCount;
            FSpritesTexturePath.SliceCount = spritesCount;

            FSpritesPositionCounts.SliceCount = spritesCount;
            FSpritesSizeCounts.SliceCount = spritesCount;
            FSpritesColorCounts.SliceCount = spritesCount;

            FTotalSpritesPositionCount = 0;
            FTotalSpritesSizeCount = 0;
            FTotalSpritesColorCount = 0;

            //sprites geometry output
            if (spritesCount != FOldSpritesGeometryOutCount || this.FFirst)
            {
                this.FInvalidate = true;

                //Dispose old
                this.FSpritesGeometryOutput.SafeDisposeAll();

                this.FSpritesGeometryOutput.SliceCount = spritesCount;
                this.FOldSpritesGeometryOutCount = spritesCount;

                for (int i = 0; i < spritesCount; i++)
                {
                    this.FSpritesGeometryOutput[i] = new DX11Resource<IDX11Geometry>();
                }
            }

            for (int i = 0; i < spritesCount; i++)
            {
                var desc = descriptions[i];

                var transCount = desc.Positions.Count;
                var sizeCount = desc.Sizes.Count;
                var colCount = desc.Colors.Count;

                FSpritesPositionCounts[i] = transCount;
                FSpritesSizeCounts[i] = sizeCount;
                FSpritesColorCounts[i] = colCount;

                FTotalSpritesPositionCount += transCount;
                FTotalSpritesSizeCount += sizeCount;
                FTotalSpritesColorCount += colCount;

                FSpritesTransformations[i] = ToSlimDXMatrix(desc.Transformation);
                FSpritesSpaceIndex[i] = (int)desc.Space;
                FSpritesTexturePath[i] = desc.TexturePath;
            }
        }

        private void UpdateNormalTextPins()
        {
            var textDescriptions = FMainBuffer.TextDescriptions;
            var textCount = textDescriptions.Count;

            FTexts.SliceCount = textCount;
            FTextTransformations.SliceCount = textCount;
            FTextSpaceIndex.SliceCount = textCount;
            FTextColors.SliceCount = textCount;
            FTextSizes.SliceCount = textCount;
            FTextFontNames.SliceCount = textCount;

            for (int i = 0; i < textCount; i++)
            {
                var desc = textDescriptions[i];

                FTexts[i] = desc.Text;
                FTextSizes[i] = desc.Size;
                FTextFontNames[i] = desc.FontName;
                FTextTransformations[i] = ToSlimDXMatrix(desc.Transformation);
                FTextSpaceIndex[i] = (int)desc.Space;
                FTextColors[i] = ToRGBAColor(desc.Color);
            }
        }

        public void Update(DX11RenderContext context)
        {
            if (this.FLayerInSpreadMax == 0) { return; }

            var newContext = !this.FTransformOutput[0].Contains(context);

            if (this.FInvalidate || newContext)
            {
                var bufferTypeChanged = this.currentBufferType != this.FBufferType[0];

                //refresh buffers?
                CheckBufferDispose<Matrix>(context, this.FTransformOutput[0], FTotalTransformCount, bufferTypeChanged);
                CheckBufferDispose<Color4>(context, this.FColorOutput[0], FTotalColorCount, bufferTypeChanged);

                CheckBufferDispose<Vector3>(context, this.FSpritesPositionOutput[0], FTotalSpritesPositionCount, bufferTypeChanged);
                CheckBufferDispose<Vector2>(context, this.FSpritesSizeOutput[0], FTotalSpritesSizeCount, bufferTypeChanged);
                CheckBufferDispose<Color4>(context, this.FSpritesColorOutput[0], FTotalSpritesColorCount, bufferTypeChanged);

                PrepareLocalGeometryBufferData(context);
                PrepareLocalSpriteBufferData(context);

                //make new buffers?
                CreateBuffer<Matrix>(FTransformOutput[0], context, FTotalTransformCount, FBufferTrans);
                CreateBuffer<Color4>(FColorOutput[0], context, FTotalColorCount, FBufferColor);

                CreateBuffer<Vector3>(FSpritesPositionOutput[0], context, FTotalSpritesPositionCount, FBufferSpritesPosition);
                CreateBuffer<Vector2>(FSpritesSizeOutput[0], context, FTotalSpritesSizeCount, FBufferSpritesSize);
                CreateBuffer<Color4>(FSpritesColorOutput[0], context, FTotalSpritesColorCount, FBufferSpritesColor);


                //if (FTotalTransformCount < 0 || FTotalColorCount < 0)
                //{
                //    this.FValid[0] = false;
                //}
            }

            this.FValid[0] = true;
            this.currentBufferType = this.FBufferType[0];

            //write to buffers
            bool needContextCopy = this.FBufferType[0] != DX11BufferUploadType.Immutable;
            if (needContextCopy)
            {
                try
                {
                    WriteToBuffer(FTransformOutput[0], context, FBufferTrans, FTotalTransformCount);
                    WriteToBuffer(FColorOutput[0], context, FBufferColor, FTotalColorCount);

                    WriteToBuffer(FSpritesPositionOutput[0], context, FBufferSpritesPosition, FTotalSpritesPositionCount);
                    WriteToBuffer(FSpritesSizeOutput[0], context, FBufferSpritesSize, FTotalSpritesSizeCount);
                    WriteToBuffer(FSpritesColorOutput[0], context, FBufferSpritesColor, FTotalSpritesColorCount);
                }
                catch (Exception ex)
                {
                    this.pluginHost.Log(TLogType.Error, ex.Message);
                }
            }
        }

        private static void CheckBufferDispose<T>(DX11RenderContext context, DX11Resource<IDX11ReadableStructureBuffer> bufferResource, int bufferCount, bool bufferTypeChanged)
            where T : struct
        {
            if (bufferResource.Contains(context))
            {
                if (bufferResource[context].ElementCount < bufferCount
                    || bufferTypeChanged
                    || bufferResource[context] is DX11ImmutableStructuredBuffer<T>)
                {
                    bufferResource.Dispose(context);
                }
            }
        }

        /// <summary>
        /// Creates a buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bufferResource">The buffer resource</param>
        /// <param name="context">The DX11 context.</param>
        /// <param name="count">The required count. Gets blown up to the next power of 2.</param>
        /// <param name="bufferToCopy">The buffer to copy in case of immutable buffer type</param>
        private void CreateBuffer<T>(DX11Resource<IDX11ReadableStructureBuffer> bufferResource, DX11RenderContext context, int count, T[] bufferToCopy)
            where T : struct
        {
            if (!bufferResource.Contains(context))
            {
                count = NextUpperPow2(count);
                if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
                {
                    bufferResource[context] = new DX11DynamicStructuredBuffer<T>(context, count);
                }
                else if (this.FBufferType[0] == DX11BufferUploadType.Default)
                {
                    bufferResource[context] = new DX11CopyDestStructuredBuffer<T>(context, count);
                }
                else
                {
                    bufferResource[context] = new DX11ImmutableStructuredBuffer<T>(context.Device, bufferToCopy, count);
                }
            }
        }

        private void WriteToBuffer<T>(DX11Resource<IDX11ReadableStructureBuffer> bufferResource, DX11RenderContext context, T[] bufferToCopy, int elementCount)
            where T : struct
        {
            if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
            {
                DX11DynamicStructuredBuffer<T> b = (DX11DynamicStructuredBuffer<T>)bufferResource[context];
                b.WriteData(bufferToCopy, 0, elementCount);
            }
            else if (this.FBufferType[0] == DX11BufferUploadType.Default)
            {
                DX11CopyDestStructuredBuffer<T> b = (DX11CopyDestStructuredBuffer<T>)bufferResource[context];
                b.WriteData(bufferToCopy, 0, elementCount);
            }
        }

        private void PrepareLocalGeometryBufferData(DX11RenderContext context)
        {
            //make sure arrays are big enough
            EnsureArraySize(ref this.FBufferTrans, FTotalTransformCount);
            EnsureArraySize(ref this.FBufferColor, FTotalColorCount);

            var descriptions = FMainBuffer.DrawDescriptions;

            var geoIndex = 0;
            var transIndex = 0;
            var colIndex = 0;
            foreach (var desc in descriptions)
            {
                var geometry = desc.GetGeometry(context);

                //check drawer
                geometry = AssignInstancedDrawer(desc, geometry);

                this.FGeometryOutput[geoIndex++][context] = geometry;

                foreach (var trans in desc.InstanceTransformations)
                {
                    trans.Transpose();
                    FBufferTrans[transIndex++] = trans;
                }

                CopyToLocalBuffer(desc.InstanceColors, FBufferColor, ref colIndex);
            }
        }

        private void PrepareLocalSpriteBufferData(DX11RenderContext context)
        {
            //make sure arrays are big enough
            EnsureArraySize(ref this.FBufferSpritesPosition, FTotalSpritesPositionCount);
            EnsureArraySize(ref this.FBufferSpritesSize, FTotalSpritesSizeCount);
            EnsureArraySize(ref this.FBufferSpritesColor, FTotalSpritesColorCount);

            var descriptions = FMainBuffer.SpritesDescriptions;

            var geoIndex = 0;
            var posIndex = 0;
            var sizeIndex = 0;
            var colIndex = 0;
            foreach (var desc in descriptions)
            {
                //null geometry
                var geometry = desc.GetGeometry(context);

                //assign drawer
                var nullGeometry = (DX11NullGeometry)geometry.ShallowCopy();
                var drawer = new DX11NullInstancedDrawer();
                drawer.VertexCount = desc.SpriteCount;
                drawer.InstanceCount = 1;
                nullGeometry.AssignDrawer(drawer);
                geometry = nullGeometry;

                this.FSpritesGeometryOutput[geoIndex++][context] = geometry;

                CopyToLocalBuffer(desc.Positions, FBufferSpritesPosition, ref posIndex);
                CopyToLocalBuffer(desc.Sizes, FBufferSpritesSize, ref sizeIndex);
                CopyToLocalBuffer(desc.Colors, FBufferSpritesColor, ref colIndex);
            }
        }

        /// <summary>
        /// Copies to local buffer and increments the index. Tries to use Array.Copy();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination array.</param>
        /// <param name="destinationStartIndex">Start index in the destination array.</param>
        private static void CopyToLocalBuffer<T>(IReadOnlyList<T> source, T[] destination, ref int destinationStartIndex)
            where T : struct
        {
            var collection = source as ICollection<T>;
            if (collection != null)
            {
                //direct copy
                collection.CopyTo(destination, destinationStartIndex);
                destinationStartIndex += collection.Count;
                return;
            }
            else 
            {
                foreach (var pos in source)
                {
                    //iteration
                    destination[destinationStartIndex++] = pos;
                }
            }
        }

        private static IDX11Geometry AssignInstancedDrawer(DrawGeometryDescription desc, IDX11Geometry geometry)
        {
            var indexedGeometry = geometry as DX11IndexedGeometry;
            if (indexedGeometry != null)
            {
                if (!(indexedGeometry.Drawer is DX11InstancedIndexedDrawer))
                {
                    indexedGeometry = (DX11IndexedGeometry)indexedGeometry.ShallowCopy();
                    var drawer = new DX11InstancedIndexedDrawer();
                    drawer.InstanceCount = desc.InstanceCount;
                    drawer.StartInstanceLocation = 0;
                    indexedGeometry.AssignDrawer(drawer);
                    geometry = indexedGeometry;
                }
            }
            else
            {
                var vertexGeometry = geometry as DX11VertexGeometry;
                if (vertexGeometry != null)
                {
                    if (!(vertexGeometry.Drawer is DX11InstancedVertexDrawer))
                    {
                        vertexGeometry = (DX11VertexGeometry)vertexGeometry.ShallowCopy();
                        var drawer = new DX11InstancedVertexDrawer();
                        drawer.InstanceCount = desc.InstanceCount;
                        drawer.StartInstanceLocation = 0;
                        vertexGeometry.AssignDrawer(drawer);
                        geometry = vertexGeometry;
                    }
                }
            }

            return geometry;
        }     

        RGBAColor ToRGBAColor(Color4 color)
        {
            return new RGBAColor(color.Red, color.Green, color.Blue, color.Alpha);
        }

        SlimDXMatrix ToSlimDXMatrix(Matrix m)
        {
            return new SlimDXMatrix()
            {
                M11 = m.M11, M12 = m.M12, M13 = m.M13, M14 = m.M14,
                M21 = m.M21, M22 = m.M22, M23 = m.M23, M24 = m.M24,
                M31 = m.M31, M32 = m.M32, M33 = m.M33, M34 = m.M34,
                M41 = m.M41, M42 = m.M42, M43 = m.M43, M44 = m.M44
            };
        }

        void EnsureArraySize<T>(ref T[] array, int minimumSize)
        {
            var newSize = array.Length;
            if (newSize < minimumSize)
            {
                do
                {
                    newSize = newSize << 1;
                }
                while (newSize < minimumSize);

                array = new T[newSize];
            }
        }

        int NextUpperPow2(int count)
        {
            var pow2 = 2;
            while (pow2 < count)
            {
                pow2 = pow2 << 1;
            }
            return pow2;
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            if (force || !this.FKeep[0])
            {
                this.FGeometryOutput.SafeDisposeAll(context);
                this.FTransformOutput.SafeDisposeAll(context);
                this.FColorOutput.SafeDisposeAll(context);

                this.FSpritesGeometryOutput.SafeDisposeAll(context);
                this.FSpritesPositionOutput.SafeDisposeAll(context);
                this.FSpritesSizeOutput.SafeDisposeAll(context);
                this.FSpritesColorOutput.SafeDisposeAll(context);
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            this.FGeometryOutput.SafeDisposeAll();
            this.FTransformOutput.SafeDisposeAll();
            this.FColorOutput.SafeDisposeAll();

            this.FSpritesGeometryOutput.SafeDisposeAll();
            this.FSpritesPositionOutput.SafeDisposeAll();
            this.FSpritesSizeOutput.SafeDisposeAll();
            this.FSpritesColorOutput.SafeDisposeAll();
        }
        #endregion

        public void OnImportsSatisfied()
        {
            this.FGeometryOutput.SliceCount = 1;
            this.FTransformOutput.SliceCount = 1;
            this.FColorOutput.SliceCount = 1;

            this.FSpritesGeometryOutput.SliceCount = 1;
            this.FSpritesPositionOutput.SliceCount = 1;
            this.FSpritesSizeOutput.SliceCount = 1;
            this.FSpritesColorOutput.SliceCount = 1;
        }
    }
}
