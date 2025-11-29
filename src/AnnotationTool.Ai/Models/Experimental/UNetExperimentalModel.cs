//using AnnotationTool.Core.Services;
//using System.Collections.Generic;
//using TorchSharp;
//using static TorchSharp.torch;
//using static TorchSharp.torch.nn;
//using static TorchSharp.torch.nn.functional;


//namespace AnnotationTool.Ai.Models.Experimental
//{
//	/// <summary>
//	/// This is just for testing purposes, not used in production
//	/// </summary>
//	public sealed class UNetExperimentalModel : Module<Tensor, Tensor>
//	{

//		#region Fields

//		// Encoder
//		private readonly Module<Tensor, Tensor> e11;
//		private readonly Module<Tensor, Tensor> e12;
//		private readonly Module<Tensor, Tensor> e13;
//		private readonly Module<Tensor, Tensor> e14;
//		private readonly Module<Tensor, Tensor> pool1;

//		private readonly Module<Tensor, Tensor> e21;
//		private readonly Module<Tensor, Tensor> e22;
//		private readonly Module<Tensor, Tensor> e23;
//		private readonly Module<Tensor, Tensor> e24;
//		private readonly Module<Tensor, Tensor> pool2;

//		private readonly Module<Tensor, Tensor> e31;
//		private readonly Module<Tensor, Tensor> e32;
//		private readonly Module<Tensor, Tensor> e33;
//		private readonly Module<Tensor, Tensor> e34;
//		private readonly Module<Tensor, Tensor> pool3;

//		private readonly Module<Tensor, Tensor> e41;
//		private readonly Module<Tensor, Tensor> e42;
//		private readonly Module<Tensor, Tensor> e43;
//		private readonly Module<Tensor, Tensor> e44;
//		private readonly Module<Tensor, Tensor> pool4;

//		// Mid
//		private readonly Module<Tensor, Tensor> e51;
//		private readonly Module<Tensor, Tensor> e52;
//		private readonly Module<Tensor, Tensor> e53;
//		private readonly Module<Tensor, Tensor> e54;

//		// Decoder
//		private readonly Module<Tensor, Tensor> upConv1;
//		private readonly Module<Tensor, Tensor> d11;
//		private readonly Module<Tensor, Tensor> d12;
//		private readonly Module<Tensor, Tensor> d13;
//		private readonly Module<Tensor, Tensor> d14;

//		private readonly Module<Tensor, Tensor> upConv2;
//		private readonly Module<Tensor, Tensor> d21;
//		private readonly Module<Tensor, Tensor> d22;
//		private readonly Module<Tensor, Tensor> d23;
//		private readonly Module<Tensor, Tensor> d24;

//		private readonly Module<Tensor, Tensor> upConv3;
//		private readonly Module<Tensor, Tensor> d31;
//		private readonly Module<Tensor, Tensor> d32;
//		private readonly Module<Tensor, Tensor> d33;
//		private readonly Module<Tensor, Tensor> d34;

//		private readonly Module<Tensor, Tensor> upConv4;
//		private readonly Module<Tensor, Tensor> d41;
//		private readonly Module<Tensor, Tensor> d42;
//		private readonly Module<Tensor, Tensor> d43;
//		private readonly Module<Tensor, Tensor> d44;

//		// Output
//		private readonly Module<Tensor, Tensor> outConv;

//		#endregion

//		public UNetExperimentalModel(IProjectPresenter project, Device device) : base(nameof(UNetExperimentalModel))
//		{

//			var uNetPara = project.Project.Settings.HyperParameters;

//			const int padding = 1;
//			const int stride = 1;

//			var filter1 = 64;
//			var filter2 = filter1 * 2;
//			var filter3 = filter2 * 2;
//			var filter4 = filter3 * 2;
//			var filter5 = filter4 * 2;

//			var inChannels = project.Project.Settings.PreprocessingSettings.TrainAsGreyscale ? 1 : 3;
//            var outChannels = project.Project.Features.Count;


//            #region Model

//            // Encoder
//            e11 = Conv2d(inChannels, filter1, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e12 = BatchNorm2d(filter1, device: device, dtype: uNetPara.TrainPrecision);
//			e13 = Conv2d(filter1, filter1, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e14 = BatchNorm2d(filter1, device: device, dtype: uNetPara.TrainPrecision);
//			pool1 = MaxPool2d(2, 2);

//			e21 = Conv2d(filter1, filter2, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e22 = BatchNorm2d(filter2, device: device, dtype: uNetPara.TrainPrecision);
//			e23 = Conv2d(filter2, filter2, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e24 = BatchNorm2d(filter2, device: device, dtype: uNetPara.TrainPrecision);
//			pool2 = MaxPool2d(2, 2);

//			e31 = Conv2d(filter2, filter3, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e32 = BatchNorm2d(filter3, device: device, dtype: uNetPara.TrainPrecision);
//			e33 = Conv2d(filter3, filter3, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e34 = BatchNorm2d(filter3, device: device, dtype: uNetPara.TrainPrecision);
//			pool3 = MaxPool2d(2, 2);

//			e41 = Conv2d(filter3, filter4, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e42 = BatchNorm2d(filter4, device: device, dtype: uNetPara.TrainPrecision);
//			e43 = Conv2d(filter4, filter4, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e44 = BatchNorm2d(filter4, device: device, dtype: uNetPara.TrainPrecision);
//			pool4 = MaxPool2d(2, 2);

//			// Mid
//			e51 = Conv2d(filter4, filter5, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e52 = BatchNorm2d(filter5, device: device, dtype: uNetPara.TrainPrecision);
//			e53 = Conv2d(filter5, filter5, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			e54 = BatchNorm2d(filter5, device: device, dtype: uNetPara.TrainPrecision);

//			// Decoder
//			upConv1 = ConvTranspose2d(filter5, filter4, 2, 2, device: device, dtype: uNetPara.TrainPrecision);
//			d11 = Conv2d(filter5, filter4, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d12 = BatchNorm2d(filter4, device: device, dtype: uNetPara.TrainPrecision);
//			d13 = Conv2d(filter4, filter4, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d14 = BatchNorm2d(filter4, device: device, dtype: uNetPara.TrainPrecision);

//			upConv2 = ConvTranspose2d(filter4, filter3, 2, 2, device: device, dtype: uNetPara.TrainPrecision);
//			d21 = Conv2d(filter4, filter3, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d22 = BatchNorm2d(filter3, device: device, dtype: uNetPara.TrainPrecision);
//			d23 = Conv2d(filter3, filter3, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d24 = BatchNorm2d(filter3, device: device, dtype: uNetPara.TrainPrecision);

//			upConv3 = ConvTranspose2d(filter3, filter2, 2, 2, device: device, dtype: uNetPara.TrainPrecision);
//			d31 = Conv2d(filter3, filter2, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d32 = BatchNorm2d(filter2, device: device, dtype: uNetPara.TrainPrecision);
//			d33 = Conv2d(filter2, filter2, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d34 = BatchNorm2d(filter2, device: device, dtype: uNetPara.TrainPrecision);

//			upConv4 = ConvTranspose2d(filter2, filter1, 2, 2, device: device, dtype: uNetPara.TrainPrecision);
//			d41 = Conv2d(filter2, filter1, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d42 = BatchNorm2d(filter1, device: device, dtype: uNetPara.TrainPrecision);
//			d43 = Conv2d(filter1, filter1, 3, stride, padding, device: device, dtype: uNetPara.TrainPrecision);
//			d44 = BatchNorm2d(filter1, device: device, dtype: uNetPara.TrainPrecision);

//			// Last layer output
//			outConv = Conv2d(filter1, outChannels, 1, device: device, dtype: uNetPara.TrainPrecision);

//			// KernelSize 1 => binary

//			#endregion

//			RegisterComponents();

//			this.to(device, true);
//		}

//		public override Tensor forward(Tensor input)
//		{
//			using (var _ = NewDisposeScope())
//			{
//				// Encoder
//				var xe11 = e11.call(input);
//				var xe12 = relu(e12.call(xe11));
//				var xe13 = e13.call(xe12);
//				var xe14 = relu(e14.call(xe13));
//				var xp1 = pool1.call(xe14);

//				var xe21 = e21.call(xp1);
//				var xe22 = relu(e22.call(xe21));
//				var xe23 = e23.call(xe22);
//				var xe24 = relu(e24.call(xe23));
//				var xp2 = pool2.call(xe24);

//				var xe31 = e31.call(xp2);
//				var xe32 = relu(e32.call(xe31));
//				var xe33 = e33.call(xe32);
//				var xe34 = relu(e34.call(xe33));
//				var xp3 = pool3.call(xe34);

//				var xe41 = e41.call(xp3);
//				var xe42 = relu(e42.call(xe41));
//				var xe43 = e43.call(xe42);
//				var xe44 = relu(e44.call(xe43));
//				var xp4 = pool4.call(xe44);

//				// Mid
//				var xe51 = e51.call(xp4);
//				var xe52 = relu(e52.call(xe51));
//				var xe53 = e53.call(xe52);
//				var xe54 = relu(e54.call(xe53));

//				// Decoder
//				var xu1 = upConv1.call(xe54);
//				var xu11 = cat(new List<Tensor>() { xu1, xe44 }, 1);
//				var xd11 = d11.call(xu11);
//				var xd12 = relu(d12.call(xd11));
//				var xd13 = d13.call(xd12);
//				var xd14 = relu(d14.call(xd13));
//				var xu2 = upConv2.call(xd14);
//				var xu22 = cat(new List<Tensor>() { xu2, xe34 }, 1);
//				var xd21 = d21.call(xu22);
//				var xd22 = relu(d22.call(xd21));
//				var xd23 = d23.call(xd22);
//				var xd24 = relu(d24.call(xd23));
//				var xu3 = upConv3.call(xd24);
//				var xu33 = cat(new List<Tensor>() { xu3, xe24 }, 1);
//				var xd31 = d31.call(xu33);
//				var xd32 = relu(d32.call(xd31));
//				var xd33 = d33.call(xd32);
//				var xd34 = relu(d34.call(xd33));
//				var xu4 = upConv4.call(xd34);
//				var xu44 = cat(new List<Tensor>() { xu4, xe14 }, 1);
//				var xd41 = d41.call(xu44);
//				var xd42 = relu(d42.call(xd41));
//				var xd43 = d43.call(xd42);
//				var xd44 = relu(d44.call(xd43));

//				// Output
//				return outConv.call(xd44).MoveToOuterDisposeScope();
//			}
//		}

//		protected override void Dispose(bool disposing)
//		{
//			if (disposing)
//			{
//				#region Dispose
//				e11.Dispose();
//				e12.Dispose();
//				e13.Dispose();
//				e14.Dispose();
//				pool1.Dispose();
//				e21.Dispose();
//				e22.Dispose();
//				e23.Dispose();
//				e24.Dispose();
//				pool2.Dispose();
//				e31.Dispose();
//				e32.Dispose();
//				e33.Dispose();
//				e34.Dispose();
//				pool3.Dispose();
//				e41.Dispose();
//				e42.Dispose();
//				e43.Dispose();
//				e44.Dispose();
//				pool4.Dispose();
//				e51.Dispose();
//				e52.Dispose();
//				e53.Dispose();
//				e54.Dispose();
//				upConv1.Dispose();
//				d11.Dispose();
//				d12.Dispose();
//				d13.Dispose();
//				d14.Dispose();
//				upConv2.Dispose();
//				d21.Dispose();
//				d22.Dispose();
//				d23.Dispose();
//				d24.Dispose();
//				upConv3.Dispose();
//				d31.Dispose();
//				d32.Dispose();
//				d33.Dispose();
//				d34.Dispose();
//				upConv4.Dispose();
//				d41.Dispose();
//				d42.Dispose();
//				d43.Dispose();
//				d44.Dispose();
//				outConv.Dispose();
//				#endregion
//				ClearModules();
//			}
//			base.Dispose(disposing);
//		}
//	}
//}