using System;
using System.Collections.Generic;
using System.Drawing;

namespace AnnotationTool.Core.Models
{
	/// <summary>
	/// persistent domain model (what exists). DTO
	/// Serializable data: image entries(Guid, relative path), ROI, split, notes, any per-item metadata.
	/// No disposable resources, no GDI objects, no caches, no UI concerns.
	/// </summary>
	public sealed class DeepLearningProject
    {
        public List<ImageItem> Images { get; set; } = new List<ImageItem>();

        public List<Feature> Features { get; set; } = new List<Feature>();

        public DeepLearningSettings Settings { get; set; } = new DeepLearningSettings();

        public long CpuMemoryBudgetBytes { get; set; }

        public long GpuMemoryBudgetBytes { get; set; }

        public DeepLearningProject()
        {
			
		}

		public void CopyFrom(DeepLearningProject other)
		{
			if (other == null) return;

			this.Features = other.Features;
			this.Images = other.Images;
			this.Settings.CopyFrom(other.Settings);
		}
	}

	public sealed class ImageItem
	{
		/// <summary>Stable id for cross-referencing bitmaps, masks, etc.</summary>
		public Guid Guid { get; set; } = Guid.NewGuid();

		/// <summary>Absolute or project-relative path to the source image.</summary>
		public string Path { get; set; } = string.Empty;

		/// <summary>Optional path to the mask on disk (PNG-8 palette recommended). If empty, mask is managed in-memory.</summary>
		public string MaskPath { get; set; }

		/// <summary>Optional Region of Interest in image pixel coordinates.</summary>
		public Rectangle Roi { get; set; }

		/// <summary>Dataset split/category.</summary>
		public DatasetSplit Split { get; set; } = DatasetSplit.Train;

		/// <summary>Optional freeform notes or training result tag.</summary>
		public SegmentationStats SegmentationStats { get; set; } = new SegmentationStats();

		/// <summary>When was this image added to the project.</summary>
		public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.Now;
	}

	public sealed class Feature
	{
		/// <summary>Zero-based class id. Convention: 0 = background.</summary>
		public int Id { get; set; }

		/// <summary>Display name of the class.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>ARGB color stored as 32-bit integer for stable JSON (e.g., 0xFFRRGGBB).</summary>
		public int Argb { get; set; }
	}

	public class SegmentationStats
	{
		/// <summary>
		/// True Positives: predicted = 1 & ground truth = 1
		/// </summary>
		public int TP { get; set; }

		/// <summary>
		/// False Positives: predicted = 1 & ground truth = 0
		/// </summary>
		public int FP { get; set; }

		/// <summary>
		/// False Negatives: predicted = 0 & ground truth = 1
		/// </summary>
		public int FN { get; set; }

		/// <summary>
		/// True Negatives: predicted = 0 & ground truth = 0
		/// </summary>
		public int TN { get; set; }

		/// <summary>
		/// (TP + TN) / (TP + TN + FP + FN) := Fraction of correctly classified pixels
		/// </summary>
		public double Accuracy { get; set; }

		/// <summary>
		/// TP / (TP + FP) := How reliable positive predictions are
		/// </summary>
		public double Precision { get; set; }

		/// <summary>
		/// TP / (TP + FN) := How much of the true object was found
		/// </summary>
		public double Recall { get; set; } /// Sensitivity

		/// <summary>
		/// TN / (TN + FP) := How well false positives are avoided
		/// </summary>
		public double Specificity { get; set; }

		/// <summary>
		/// 2·TP / (2·TP + FP + FN) := Overlap between predicted and ground truth masks
		/// </summary>
		public double Dice { get; set; } /// F1 Score

		/// <summary>
		/// TP / (TP + FP + FN) := Intersection over Union between predicted and ground truth masks
		/// </summary>
		public double IoU { get; set; }

		/// <summary>
		/// FP / (FP + TN) := Fraction of negative pixels incorrectly classified as positive
		/// </summary>
		public double FPR { get; set; }

		/// <summary>
		/// Computation time in milliseconds per image during inference.
		/// </summary>
		public double InferenceMs { get; set; }
	}
}
