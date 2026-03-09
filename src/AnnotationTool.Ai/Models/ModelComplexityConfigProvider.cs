using AnnotationTool.Core.Models;
using static TorchSharp.torch;

namespace AnnotationTool.Ai.Models
{
    ///						+--------------------------------------------------------------+
    ///						|            Segmentation (UNet) Model Complexity              |
    ///						+--------------------------------------------------------------+
    ///
    ///			LOW COMPLEXITY                       MEDIUM COMPLEXITY                     HIGH COMPLEXITY
    ///		    ----------------                     -----------------                     ----------------
    ///         Shallow UNet                         Standard UNet                         Deep UNet
    ///         Depth = 2                            Depth = 3                             Depth = 4
    ///         Filters: 32→64                       Filters: 64→128→256                   Filters: 96→192→384→768
    ///
    ///			Downsampling: MaxPool			  	 Downsampling: StridedConv		  	   Downsampling: StridedConv
    ///			Upsampling: Interpolation		  	 Upsampling: Interpolation			   Upsampling: ConvTranspose2d
    ///
    ///			Attention: OFF					  	 Attention: Optional				   Attention: ON
    ///			Dropout: OFF					  	 Dropout: OFF					       Dropout:  ON
    ///
    ///			Model Size: Small				  	 Model Size: Medium					   Model Size: Large
    ///			Speed: Fast					         Speed: Moderate					   Speed: Slowest
    ///			Detail: Basic					     Detail: Good					       Detail: Maximum
    ///
    ///           ┌─────────┐                       ┌─────────┐                            ┌─────────┐
    ///  Input →  │ Encoder │ → Bottleneck →        │ Encoder │ → Bottleneck →             │ Encoder │ → Bottleneck →
    ///           └─┬─────┬─┘                       └─┬─────┬─┘                            └─┬─────┬─┘
    ///             ↓     ↓                           ↓     ↓                                ↓     ↓
    ///            Max   Max                        Stride Stride                          Stride Stride
    ///            Pool  Pool                       Conv   Conv                            Conv   Conv
    ///             ↓     ↓                           ↓     ↓                                ↓     ↓
    ///            Up    Up                           Up    Up                              Up     Up
    ///          (Interpolation)                  (Interp or ConvT)                       (ConvTranspose2d)
    ///               ↓                               ↓                                         ↓
    ///             Output                          Output                                    Output
    ///                                                                                
    ///                
    /// !!!!!!!!!!    Check ModelComplexityConfigFactory for current deployed config values.    !!!!!!!!!! 
    /// 
    public class ModelComplexityConfigProvider : IModelComplexityConfigProvider
    {
        public SegmentationModelConfig GetConfig(ModelComplexity complexity, int imageWidth, int imageHeight)
        {
            var cfg = new SegmentationModelConfig();

            //cfg.TrainPrecision = ScalarType.BFloat16;
            cfg.TrainPrecision = ScalarType.Float32;

            if (complexity == ModelComplexity.Low)
            {
                cfg.Depth = 2;
                cfg.FirstFilter = 32;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = false;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.Medium)
            {

                cfg.Depth = 3;
                cfg.FirstFilter = 64;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = true;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.High)
            {
                cfg.Depth = 4;
                cfg.FirstFilter = 96;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = true;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
            }
            return cfg;
        }
    }
}
