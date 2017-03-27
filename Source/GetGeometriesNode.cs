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

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "GetGeometries", Category = "DX11.Buffer", Author = "tonfilm")]
    public class GetGeometriesNode : IPluginEvaluate, IDX11ResourceHost, IPartImportsSatisfiedNotification, IDisposable
    {
        [Import()]
        protected IPluginHost2 pluginHost;

        [Input("Buffer Drawer", Order=5)]
        protected IDiffSpread<BufferDrawer> FDrawerIn;

        [Input("Keep In Memory", DefaultValue = 0, Order=6)]
        protected ISpread<bool> FKeep;

        [Input("Preferred Buffer Type", DefaultValue = 0, Order = 6, Visibility = PinVisibility.OnlyInspector)]
        protected ISpread<DX11BufferUploadType> FBufferType;

        [Input("Apply", IsBang = true, DefaultValue = 1,Order=7)]
        protected ISpread<bool> FApply;

        [Output("Geometry Out")]
        protected Pin<DX11Resource<DX11IndexedGeometry>> FOutput;

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

        DX11BufferUploadType currentBufferType = DX11BufferUploadType.Dynamic;

        [Output("Is Valid")]
        protected ISpread<bool> FValid;

        private bool FInvalidate;
        private bool FFirst = true;
        private int spreadmax;

        protected virtual bool NeedConvert { get { return false; } }

        BufferDrawer FMainDrawer;

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
                        FMainDrawer = BufferDrawer.Unite(FDrawerIn);
                    }
                    else
                    {
                        FMainDrawer = FDrawerIn[0];
                    }

                    if (FMainDrawer == null)
                        FMainDrawer = BufferDrawer.Default;
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

                this.spreadmax = this.FDrawerIn.SliceCount;

                this.FInvalidate = true;
                this.FFirst = false;
                this.FTransformOutput.Stream.IsChanged = true;
                this.FColorOutput.Stream.IsChanged = true;

                //set all the count outputs and get the total counts
                UpdatePins();
            }

        }

        int FTotalTransformCount;
        int FTotalColorCount;

        protected Matrix[] FBufferTrans = new Matrix[4096];
        protected Color4[] FBufferCol = new Color4[4096];

        public void Update(DX11RenderContext context)
        {
            if (this.spreadmax == 0) { return; }

            if (this.FInvalidate || !this.FTransformOutput[0].Contains(context))
            {              
                //refresh trans buffer?
                if (this.FTransformOutput[0].Contains(context))
                {
                    if (this.FTransformOutput[0][context].ElementCount != FTotalTransformCount
                        || this.currentBufferType != this.FBufferType[0] 
                        || this.FTransformOutput[0][context] is DX11ImmutableStructuredBuffer<Matrix>)
                    {
                        this.FTransformOutput[0].Dispose(context);
                    }
                }

                //refresh col buffer?
                if (this.FColorOutput[0].Contains(context))
                {
                    if (this.FColorOutput[0][context].ElementCount != FTotalColorCount
                        || this.currentBufferType != this.FBufferType[0]
                        || this.FColorOutput[0][context] is DX11ImmutableStructuredBuffer<Color4>)
                    {
                        this.FColorOutput[0].Dispose(context);
                    }
                }

                SetupLocalBuffers(context);
               
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

        private void SetupLocalBuffers(DX11RenderContext context)
        {
            if (this.FBufferTrans.Length < FTotalTransformCount)
            {
                Array.Resize(ref this.FBufferTrans, FTotalTransformCount);
            }

            if (this.FBufferCol.Length < FTotalColorCount)
            {
                Array.Resize(ref this.FBufferCol, FTotalColorCount);
            }

            var geos = FMainDrawer.Geometries;

            var geoIndex = 0;
            var transIndex = 0;
            var colIndex = 0;
            foreach (var geo in geos)
            {
                var geom = geo.GetGeom(context);

                if (geo.InstanceCount > 1)
                {
                    var drawer = new DX11InstancedIndexedDrawer();
                    drawer.InstanceCount = geo.InstanceCount;
                    drawer.StartInstanceLocation = 0;
                    geom.AssignDrawer(drawer); 
                }

                this.FOutput[geoIndex++][context] = geom;

                foreach (var trans in geo.InstanceTransformations)
                {
                    trans.Transpose();
                    FBufferTrans[transIndex++] = trans;
                }

                foreach (var col in geo.InstanceColors)
                {
                    FBufferCol[colIndex++] = col;
                }

            }
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
            var geoms = FMainDrawer.Geometries;
            var outCount = geoms.Count;

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
                    this.FOutput[i] = new DX11Resource<DX11IndexedGeometry>();
                }
            }

            for (int i = 0; i < outCount; i++)
            {
                var geo = geoms[i];

                FInstanceCounts[i] = geo.InstanceCount;

                var transCount = geo.InstanceTransformations.Count;
                var colCount = geo.InstanceColors.Count;
                FTransformCounts[i] = transCount;
                FColorCounts[i] = colCount;

                FTransformation[i] = ToSlimDXMatrix(geo.Transformation);
                FTexturePath[i] = geo.TexturePath;

                FTotalTransformCount += transCount;
                FTotalColorCount += colCount;
            }
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
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            this.FOutput.SafeDisposeAll();
            this.FTransformOutput.SafeDisposeAll();
            this.FColorOutput.SafeDisposeAll();
        }
        #endregion

        public void OnImportsSatisfied()
        {
            this.FTransformOutput.SliceCount = 1;
            this.FColorOutput.SliceCount = 1;
        }
    }
}
