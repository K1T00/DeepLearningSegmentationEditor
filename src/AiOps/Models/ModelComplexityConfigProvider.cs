using AnnotationTool.Core.Models;

namespace AnnotationTool.Ai.Models
{
    ///
    ///                                           -----  WIP -----
    ///
    ///
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
    ///
    public class ModelComplexityConfigProvider : IModelComplexityConfigProvider
    {
        public SegmentationModelConfig GetConfig(
            ModelComplexity complexity,
            int imageWidth,
            int imageHeight)
        {
            SegmentationModelConfig cfg = new SegmentationModelConfig();

            
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

                //cfg.Depth = 2;
                //cfg.FirstFilter = 32;

                //cfg.UsePooling = true;
                //cfg.UseStridedConv = false;
                //cfg.UseInterpolationDown = false;
                //cfg.UseInterpolationUp = true;

                //cfg.UseInstanceNorm = false;
                //cfg.UseDropout = false;
                //cfg.UseChannelAttention = false;
                //cfg.UseAttentionGates = false;
                //cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.Medium)
            {

                cfg.Depth = 3;
                cfg.FirstFilter = 64;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = false;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment


                //cfg.Depth = 3;
                //cfg.FirstFilter = 64;

                //cfg.UsePooling = false;
                //cfg.UseStridedConv = true;
                //cfg.UseInterpolationDown = false;
                //cfg.UseInterpolationUp = true;

                //cfg.UseInstanceNorm = true;
                //cfg.UseDropout = false;
                //cfg.UseChannelAttention = false;
                //cfg.UseAttentionGates = false;
                //cfg.UseSelfAttention = false; // Not working at the moment
            }
            else if (complexity == ModelComplexity.High)
            {
                cfg.Depth = 4;
                cfg.FirstFilter = 96;

                cfg.UsePooling = true;
                cfg.UseStridedConv = false;
                cfg.UseInterpolationDown = false;
                cfg.UseInterpolationUp = false;

                cfg.UseInstanceNorm = false;
                cfg.UseDropout = false;
                cfg.UseChannelAttention = false;
                cfg.UseAttentionGates = false;
                cfg.UseSelfAttention = false; // Not working at the moment
                //cfg.Depth = 4;
                //cfg.FirstFilter = 96;

                //cfg.UsePooling = false;
                //cfg.UseStridedConv = true;
                //cfg.UseInterpolationDown = false;
                //cfg.UseInterpolationUp = false;

                //cfg.UseInstanceNorm = true;
                //cfg.UseDropout = true;
                //cfg.UseChannelAttention = true;
                //cfg.UseAttentionGates = true;
                //cfg.UseSelfAttention = true; // Not working at the moment
            }

            // Apply automatic resolution scaling
            //ApplyAutoScaling(cfg, imageWidth, imageHeight);

            return cfg;
        }

        private void ApplyAutoScaling(SegmentationModelConfig cfg, int width, int height)
        {
            int maxDim = width > height ? width : height;

            if (maxDim <= 256)
            {
                if (cfg.Depth > 2) cfg.Depth = 2;
                if (cfg.FirstFilter > 32) cfg.FirstFilter = 32;
            }
            else if (maxDim <= 512)
            {
                if (cfg.Depth > 3) cfg.Depth = 3;
                if (cfg.FirstFilter > 48) cfg.FirstFilter = 48;
            }
            else if (maxDim <= 1024)
            {
                if (cfg.Depth < 4) cfg.Depth = 4;
                if (cfg.FirstFilter < 64) cfg.FirstFilter = 64;
            }
            else // >1024px
            {
                if (cfg.Depth < 5) cfg.Depth = 5;
                if (cfg.FirstFilter < 96) cfg.FirstFilter = 96;

                cfg.UseChannelAttention = true;
                cfg.UseAttentionGates = true;
                cfg.UseSelfAttention = true;

                cfg.UseInterpolationDown = true;
            }
        }
    }
}
