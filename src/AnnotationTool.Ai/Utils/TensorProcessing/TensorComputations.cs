using System;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace AnnotationTool.Ai.Utils.TensorProcessing
{
    public static class TensorComputations
    {

        /// <summary>
        /// Replacement for torchvision.functional.affine using core TorchSharp ops.
        /// https://github.com/dotnet/TorchSharp/pull/1547#event-23220838956 obsolete?
        /// Works for 3D [C,H,W] or 4D [N,C,H,W].
        /// </summary>
        /// 
        public static Tensor SafeAffine(Tensor img, float angle, int[] translate, float scale, float[] shear, InterpolationMode mode)
        {
            if (translate == null || translate.Length != 2)
                throw new ArgumentException("translate must contain exactly 2 values: [tx, ty].", nameof(translate));

            if (shear == null)
                throw new ArgumentNullException(nameof(shear));

            if (img.shape.Length != 3 && img.shape.Length != 4)
                throw new ArgumentException("img must have shape [C,H,W] or [N,C,H,W].", nameof(img));

            if (scale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(scale), "scale must be > 0.");

            var scope = NewDisposeScope();
            try
            {
                var device = img.device;
                var originalDtype = img.dtype;

                var addedBatchDimension = img.shape.Length == 3;
                var working = addedBatchDimension ? img.unsqueeze(0) : img;

                // grid_sample requires floating-point input
                if (!working.is_floating_point())
                    working = working.to_type(ScalarType.Float32);

                var height = (float)working.shape[2];
                var width = (float)working.shape[3];

                var angleRad = angle * (float)Math.PI / 180f;
                var shearXRad = (shear.Length > 0 ? shear[0] : 0f) * (float)Math.PI / 180f;
                var shearYRad = (shear.Length > 1 ? shear[1] : 0f) * (float)Math.PI / 180f;

                var cosA = (float)Math.Cos(angleRad);
                var sinA = (float)Math.Sin(angleRad);
                var tanShearX = (float)Math.Tan(shearXRad);
                var tanShearY = (float)Math.Tan(shearYRad);

                // Pixel translation -> normalized affine_grid coordinates.
                // align_corners:false is used consistently in both affine_grid and grid_sample.
                var txNorm = translate[0] / (width / 2f);
                var tyNorm = translate[1] / (height / 2f);

                var thetaData = new float[,]
                {
                    {
                        scale * cosA + tanShearY * sinA,
                        -scale * sinA + tanShearX * cosA,
                        txNorm
                    },
                    {
                        scale * sinA,
                        scale * cosA,
                        tyNorm
                    }
                };

                var theta = tensor(thetaData, dtype: ScalarType.Float32, device: device).reshape(1, 2, 3);
                try
                {
                    var grid = functional.affine_grid(theta, working.shape, align_corners: false);
                    try
                    {
                        var gridMode = mode == InterpolationMode.Bilinear
                            ? GridSampleMode.Bilinear
                            : GridSampleMode.Nearest;

                        var output = functional.grid_sample(
                            working,
                            grid,
                            mode: gridMode,
                            padding_mode: GridSamplePaddingMode.Zeros,
                            align_corners: false);

                        if (addedBatchDimension)
                            output = output.squeeze(0);

                        if (mode == InterpolationMode.Nearest &&
                            !img.is_floating_point() &&
                            output.dtype != originalDtype)
                        {
                            output = output.to_type(originalDtype);
                        }

                        return output.MoveToOuterDisposeScope();
                    }
                    finally
                    {
                        grid.Dispose();
                    }
                }
                finally
                {
                    theta.Dispose();
                }
            }
            finally
            {
                scope.Dispose();
            }
        }
    }
}
