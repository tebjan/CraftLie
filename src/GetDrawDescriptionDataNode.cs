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

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "GetDrawDescriptionData", Category = "DX11.Buffer", Version = "CraftLie", Author = "tonfilm")]
    public class GetDrawDescriptionDataNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification, IDisposable
    {
        [Import()]
        protected IPluginHost2 pluginHost;

        [Input("Buffer", Order=5)]
        protected IDiffSpread<DrawDescriptionLayer> FDrawerIn;

        [Input("Keep In Memory", DefaultValue = 0, Order=6)]
        protected ISpread<bool> FKeep;

        [Input("Preferred Buffer Type", DefaultValue = 0, Order = 6, Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11BufferUploadType> FBufferType;

        [Input("Apply", IsBang = true, DefaultValue = 1,Order=7)]
        protected ISpread<bool> FApply;

        [Output("Geometry Out")]
        protected Pin<DX11Resource<IDX11Geometry>> FOutput;

        [Output("Instance Counts")]
        protected ISpread<int> FInstanceCounts;

        [Output("Transformation")]
        protected ISpread<SlimDXMatrix> FTransformation;

        [Output("Texture Path")]
        protected ISpread<string> FTexturePath;

        [Output("Transform Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FTransformOutput;

        [Output("Transform Counts")]
        protected ISpread<int> FTransformCounts;

        [Output("Color Buffer")]
        protected ISpread<DX11Resource<IDX11ReadableStructureBuffer>> FColorOutput;

        [Output("Color Counts")]
        protected ISpread<int> FColorCounts;

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

        [Output("Texts")]
        protected ISpread<string> FTexts;

        [Output("Text Transformations")]
        protected ISpread<SlimDXMatrix> FTextTransformations;

        [Output("Text Colors")]
        protected ISpread<RGBAColor> FTextColors;

        [Output("Text Sizes")]
        protected ISpread<float> FTextSizes;

        [Output("Font Names")]
        protected ISpread<string> FFontNames;

        DX11BufferUploadType currentBufferType = DX11BufferUploadType.Dynamic;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        private bool FInvalidate;
        private bool FFirst = true;
        private int FGeometrySpreadMax;

        protected virtual bool NeedConvert { get { return false; } }

        DrawDescriptionLayer FMainBuffer;

        protected int oldOutCount = 0;

        public void Evaluate(int SpreadMax)
        {
            this.FInvalidate = false;

            //buffer outputs
            if (this.FApply[0] || this.FFirst)
            {
                if (this.FDrawerIn.SliceCount > 0)
                {
                    this.FTransformOutput.SliceCount = 1;
                    this.FColorOutput.SliceCount = 1;

                    this.FValid.SliceCount = 1;

                    if (this.FTransformOutput[0] == null) { this.FTransformOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }
                    if (this.FColorOutput[0] == null) { this.FColorOutput[0] = new DX11Resource<IDX11ReadableStructureBuffer>(); }

                    if(FDrawerIn.SliceCount > 1)
                    {
                        FMainBuffer = DrawDescriptionLayer.Unite(FDrawerIn);
                    }
                    else
                    {
                        FMainBuffer = FDrawerIn[0];
                    }

                    if (FMainBuffer == null)
                        FMainBuffer = DrawDescriptionLayer.Default;
                }
                else
                {
                    if (this.FTransformOutput.SliceCount > 0 && this.FTransformOutput[0] != null)
                    {
                        this.FTransformOutput[0].Dispose();
                    }
                    this.FTransformOutput.SliceCount = 0;

                    if (this.FColorOutput.SliceCount > 0 && this.FColorOutput[0] != null)
                    {
                        this.FColorOutput[0].Dispose();
                    }
                    this.FColorOutput.SliceCount = 0;

                    this.FInstanceCounts.SliceCount = 0;
                    this.FTexturePath.SliceCount = 0;
                    this.FTransformCounts.SliceCount = 0;
                    this.FColorCounts.SliceCount = 0;

                    this.FValid.SliceCount = 0;
                }

                this.FGeometrySpreadMax = this.FDrawerIn.SliceCount;

                this.FInvalidate = true;
                this.FFirst = false;
                this.FTransformOutput.Stream.IsChanged = true;
                this.FColorOutput.Stream.IsChanged = true;

                //set all the count output pins and get the total counts
                UpdatePins();
            }

        }

        int FTotalTransformCount;
        int FTotalColorCount;
        int FTotalSpritesCount;

        protected Matrix[] FBufferTrans = new Matrix[4096];
        protected Color4[] FBufferCol = new Color4[4096];

        protected Vector3[] FBufferSpritesPosition = new Vector3[8192];
        protected Vector2[] FBufferSpritesSize = new Vector2[8192];
        protected Color4[] FBufferSpritesCol = new Color4[8192];

        public void Update(DX11RenderContext context)
        {
            if (this.FGeometrySpreadMax == 0) { return; }

            if (this.FInvalidate || !this.FTransformOutput[0].Contains(context))
            {
                var bufferTypeChanged = this.currentBufferType != this.FBufferType[0];

                //refresh trans buffer?
                CheckBufferDispose<Matrix>(context, this.FTransformOutput[0], FTotalTransformCount, bufferTypeChanged);

                //refresh col buffer?
                CheckBufferDispose<Color4>(context, this.FColorOutput[0], FTotalColorCount, bufferTypeChanged);

                PrepareLocalGeometryBufferData(context);

                //make new buffer?
                if (!this.FTransformOutput[0].Contains(context))
                {
                    if (FTotalTransformCount > 0 && FTotalColorCount > 0)
                    {
                        CreateBuffer<Matrix>(FTransformOutput, context, FTotalTransformCount, FBufferTrans);
                        CreateBuffer<Color4>(FColorOutput, context, FTotalColorCount, FBufferCol);

                        this.FValid[0] = true;
                        this.currentBufferType = this.FBufferType[0];
                    }
                    else
                    {
                        this.FValid[0] = false;
                        return;
                    }
                }

                bool needContextCopy = this.FBufferType[0] != DX11BufferUploadType.Immutable;
                if (needContextCopy)
                {
                    try
                    {
                        WriteToBuffer(FTransformOutput, context, FBufferTrans);
                        WriteToBuffer(FColorOutput, context, FBufferCol);
                    }
                    catch (Exception ex)
                    {
                        this.pluginHost.Log(TLogType.Error, ex.Message);
                    }
                }
            }

        }

        private static void CheckBufferDispose<T>(DX11RenderContext context, DX11Resource<IDX11ReadableStructureBuffer> bufferResource, int bufferCount, bool bufferTypeChanged)
            where T : struct
        {
            if (bufferResource.Contains(context))
            {
                if (bufferResource[context].ElementCount != bufferCount
                    || bufferTypeChanged
                    || bufferResource[context] is DX11ImmutableStructuredBuffer<T>)
                {
                    bufferResource.Dispose(context);
                }
            }
        }

        private void WriteToBuffer<T>(ISpread<DX11Resource<IDX11ReadableStructureBuffer>> spread, DX11RenderContext context, T[] bufferToCopy) where T : struct
        {
            if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
            {
                DX11DynamicStructuredBuffer<T> b = (DX11DynamicStructuredBuffer<T>)spread[0][context];
                b.WriteData(bufferToCopy, 0, b.ElementCount);
            }
            else if (this.FBufferType[0] == DX11BufferUploadType.Default)
            {
                DX11CopyDestStructuredBuffer<T> b = (DX11CopyDestStructuredBuffer<T>)spread[0][context];
                b.WriteData(bufferToCopy, 0, b.ElementCount);
            }
        }

        private void PrepareLocalGeometryBufferData(DX11RenderContext context)
        {
            if (this.FBufferTrans.Length < FTotalTransformCount)
            {
                Array.Resize(ref this.FBufferTrans, FTotalTransformCount);
            }

            if (this.FBufferCol.Length < FTotalColorCount)
            {
                Array.Resize(ref this.FBufferCol, FTotalColorCount);
            }

            var descriptions = FMainBuffer.DrawDescriptions;

            var geoIndex = 0;
            var transIndex = 0;
            var colIndex = 0;
            foreach (var desc in descriptions)
            {
                var geometry = desc.GetGeometry(context);

                //check drawer
                geometry = AssignInstancedDrawer(desc, geometry);

                this.FOutput[geoIndex++][context] = geometry;

                foreach (var trans in desc.InstanceTransformations)
                {
                    trans.Transpose();
                    FBufferTrans[transIndex++] = trans;
                }

                foreach (var col in desc.InstanceColors)
                {
                    FBufferCol[colIndex++] = col;
                }
            }
        }

        private static IDX11Geometry AssignInstancedDrawer(DrawDescription desc, IDX11Geometry geometry)
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

        private void CreateBuffer<T>(ISpread<DX11Resource<IDX11ReadableStructureBuffer>> spread, DX11RenderContext context, int count, T[] bufferToCopy) where T : struct
        {
            if (this.FBufferType[0] == DX11BufferUploadType.Dynamic)
            {
                spread[0][context] = new DX11DynamicStructuredBuffer<T>(context, count);
            }
            else if (this.FBufferType[0] == DX11BufferUploadType.Default)
            {
                spread[0][context] = new DX11CopyDestStructuredBuffer<T>(context, count);
            }
            else
            {
                spread[0][context] = new DX11ImmutableStructuredBuffer<T>(context.Device, bufferToCopy, count);
            }
        }

        private void UpdatePins()
        {
            var descriptions = FMainBuffer.DrawDescriptions;
            var outCount = descriptions.Count;

            FOutput.SliceCount = outCount;

            FInstanceCounts.SliceCount = outCount;
            FTransformCounts.SliceCount = outCount;
            FColorCounts.SliceCount = outCount;

            FTransformation.SliceCount = outCount;
            FTexturePath.SliceCount = outCount;

            FTotalTransformCount = 0;
            FTotalColorCount = 0;

            //geometry output
            if (outCount != oldOutCount || this.FFirst)
            {
                this.FInvalidate = true;

                //Dispose old
                this.FOutput.SafeDisposeAll();

                this.FOutput.SliceCount = outCount;
                this.oldOutCount = outCount;

                for (int i = 0; i < outCount; i++)
                {
                    this.FOutput[i] = new DX11Resource<IDX11Geometry>();
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

                FTransformation[i] = ToSlimDXMatrix(desc.Transformation);
                FTexturePath[i] = desc.TexturePath;

                FTotalTransformCount += transCount;
                FTotalColorCount += colCount;
            }

            var textDescriptions = FMainBuffer.TextDescriptions;
            var textCount = textDescriptions.Count;

            FTexts.SliceCount = textCount;
            FTextTransformations.SliceCount = textCount;
            FTextColors.SliceCount = textCount;
            FTextSizes.SliceCount = textCount;
            FFontNames.SliceCount = textCount;

            for (int i = 0; i < textCount; i++)
            {
                var desc = textDescriptions[i];


                FTexts[i] = desc.Text;
                FTextSizes[i] = desc.Size;
                FFontNames[i] = desc.FontName;
                FTextTransformations[i] = ToSlimDXMatrix(desc.Transformation);
                FTextColors[i] = ToRGBAColor(desc.Color);
            }
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

        public void Destroy(DX11RenderContext context, bool force)
        {
            if (force || !this.FKeep[0])
            {
                this.FOutput.SafeDisposeAll(context);
                this.FTransformOutput.SafeDisposeAll(context);
                this.FColorOutput.SafeDisposeAll(context);
                this.FSpritesPositionOutput.SafeDisposeAll(context);
                this.FSpritesSizeOutput.SafeDisposeAll(context);
                this.FSpritesColorOutput.SafeDisposeAll(context);
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            this.FOutput.SafeDisposeAll();
            this.FTransformOutput.SafeDisposeAll();
            this.FColorOutput.SafeDisposeAll();
            this.FSpritesPositionOutput.SafeDisposeAll();
            this.FSpritesSizeOutput.SafeDisposeAll();
            this.FSpritesColorOutput.SafeDisposeAll();
        }
        #endregion

        public void OnImportsSatisfied()
        {
            this.FOutput.SliceCount = 1;
            this.FTransformOutput.SliceCount = 1;
            this.FColorOutput.SliceCount = 1;
            this.FSpritesPositionOutput.SliceCount = 1;
            this.FSpritesSizeOutput.SliceCount = 1;
            this.FSpritesColorOutput.SliceCount = 1;
        }
    }
}
