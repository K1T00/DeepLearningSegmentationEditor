using System;
using System.Collections.Generic;
using TorchSharp;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;


namespace AnnotationTool.Ai.Utils
{
    // Binary segmentation: Bernoulli decision
    // Output: single channel with logits [Batch, 1, H, W]
    // Loss binary_cross_entropy_with_logits

    // Multiclass segmentation: Categorical decision
    // Output: multi-channel with logits [Batch, C, H, W]
    // Loss: cross_entropy

    // Don't use Multiclass for binary segmentation:
    // In binary-as-multiclass:
    // foreground and background compete
    // background dominates gradients(especially with class imbalance)
    // foreground gradients get weaker
    // This often leads to:
    // slower convergence
    // worse recall
    // thicker boundaries

    // Binary BCE:
    // Direct sigmoid → strong gradient when wrong
    // Better for sparse positives
    // Multiclass CE:
    // Softmax spreads probability mass
    // Wrong-class gradients can vanish faster
    // This matters in segmentation where:
    // positives are thin structures
    // edges matter
    // class imbalance is extreme

    // Therefore:
    // features.count == 1 => binary pipeline
    // features.count > 1 => multiclass pipeline

    internal static class LossFunctions
    {
        public static Tensor ComputeBinarySegmentationLoss(Tensor prediction, Tensor target)
        {
            return ComputeDiceWithBceLossBin(prediction, target);
        }
        public static Tensor ComputeMulticlassSegmentationLoss(Tensor prediction, Tensor target)
        {
            return DiceCrossEntropyLoss(prediction, target);
        }

        #region Binary Case


        /// <summary>
        /// Dice loss is used to measure the overlap between the predicted and target masks,
        /// while BCE loss is used to measure the pixel-wise accuracy.
        ///
        /// Dice = 2 * ∣ Prediction ∩ Target  ∣ Prediction ∣ + ∣ Target∣ 
        ///      =                 2TP        /     (2TP + FP + FN)
        /// 
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="target"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public static Tensor ComputeDiceWithBceLossBin(Tensor prediction, Tensor target, float smooth = 1e-6f)
        {
            // Convert logits to probabilities
            var prob = prediction.sigmoid();
            var bceLoss = functional.binary_cross_entropy_with_logits(prediction, target); // Result is scalar

            // Flatten
            var predFlat = prob.flatten(start_dim: 1).to_type(ScalarType.Float32);
            var targetFlat = target.flatten(start_dim: 1).to_type(ScalarType.Float32);

            // Compute intersection and union
            var intersection = (predFlat * targetFlat).sum();
            var sum = predFlat.sum() + targetFlat.sum();

            var diceCoeff = (2 * intersection + smooth) / (sum + smooth);
            var diceLoss = 1 - diceCoeff;

            return diceLoss + bceLoss;
        }

        /// <summary>
        ///	Dice score for binary masks
        ///
        /// Compared to IoU, the Dice score is more sensitive to small objects.
        ///
        /// Dice = 2 * ∣ Prediction ∩ Target  ∣ Prediction ∣ + ∣ Target∣ 
        ///      =                 2TP        /     (2TP + FP + FN)
        /// with TP := True Positives; FP := False Positives; FN := False Negatives
        /// </summary>
        /// <param name="prediction">Model prediction tensor.</param>
        /// <param name="target">Ground truth mask tensor.</param>
        /// <param name="threshold">Threshold for binarization</param>
        /// <returns></returns>
        public static Tensor ComputeDiceScoreBin(Tensor prediction, Tensor target, float threshold = 0.5f)
        {
            var prob = prediction.sigmoid();

            // Binarize predictions and targets
            var predictionBin = prob >= threshold;
            var targetBin = target >= 0.5;

            // Flatten tensors
            var predictionFlat = predictionBin.flatten();
            var targetFlat = targetBin.flatten();

            // Calculate intersection and union
            var intersection = (predictionFlat & targetFlat).sum().to_type(ScalarType.Float32);
            var union = (predictionFlat | targetFlat).sum().to_type(ScalarType.Float32);

            var sum = predictionFlat.sum() + targetFlat.sum();
            var epsilon = tensor(1e-6f);

            return 2 * intersection / (sum + epsilon);
        }


        /// <summary>
        /// Intersection over Union for binary masks
        ///
        /// IoU is a metric that measures how well the predicted segmentation overlaps with the ground truth mask.
        /// 1.0 means perfect overlap, while 0.0 means no overlap.
        ///
        /// IoU = ∣ Prediction ∩ Target  ∣ Prediction ∪ Target∣ 
        ///     =              TP        /    (TP + FP + FN)
        /// with TP := True Positives; FP := False Positives; FN := False Negatives
        /// </summary>
        /// <param name="prediction">Model prediction tensor.</param>
        /// <param name="target">Ground truth mask tensor.</param>
        /// <param name="threshold">Threshold for binarization</param>
        /// <returns>iou</returns>
        public static Tensor ComputeIouBin(Tensor prediction, Tensor target, float threshold = 0.5f)
        {
            var prob = prediction.sigmoid();

            // Binarize (by using threshold) predictions and targets
            var predictionBin = prob >= threshold;
            var targetBin = target >= threshold;

            // Flatten for element wise operations
            var predictionFlat = predictionBin.flatten();
            var targetFlat = targetBin.flatten();

            // Calculate intersection and union
            var intersection = (predictionFlat & targetFlat).sum().to_type(ScalarType.Float32);
            var union = (predictionFlat | targetFlat).sum().to_type(ScalarType.Float32);

            // Add epsilon to prevent division by zero
            var epsilon = tensor(1e-6f);

            return intersection / (union + epsilon);
        }

        /// <summary>
        /// 
        /// Focal loss for binary case: FL(p_t) = −α(1−p_t)^γ*log(p_t)
        ///
        /// p_t := the predicted probability of the true class (after sigmoid)
        ///   α := a weighting factor for class imbalance (e.g. 0.25)
        ///   γ := the focusing parameter (e.g. 2.0). Higher = focus more on hard cases
        ///
        /// Some rule of thumbs:
        ///
        /// If loss is too low early => lower gamma
        /// If loss stays too high => increase gamma
        /// If positive class is very rate => keep alpha low
        /// If foreground pixels are < 10% of total pixels, keep alpha around 0.25
        /// If foreground pixels are ~ 50% set alpha closer to 0.5 or 1.0
        /// 
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="targets"></param>
        /// <param name="alpha"></param>
        /// <param name="gamma"></param>
        /// <returns></returns>
        public static Tensor FocalLossBinary(Tensor prediction, Tensor targets, float alpha = 0.25f, float gamma = 1.0f)
        {
            // Convert logits to probabilities
            var prob = prediction.sigmoid();

            // Compute bce per element
            var bceLoss = functional.binary_cross_entropy_with_logits(prediction, targets, reduction: Reduction.None);

            // Compute focal loss modulating factor
            var p_t = where(targets == 1, prob, 1 - prob);
            var focalWeight = pow(1 - p_t, gamma);

            // Apply weighting and alpha balancing
            var alphaFactor = where(targets == 1, alpha, 1 - alpha);
            var loss = alphaFactor * focalWeight * bceLoss;

            return loss.mean();
        }

        /// <summary>
        /// To calculate the ROC curve (for binary case), you need the TPR (sensitivity) and FPR (1-specificity) at different thresholds.
        ///
        ///	TPR = TP / (TP + FN)
        /// FPR = FP / (FP + TN)
        /// with TP := True Positives; FP := False Positives; FN := False Negatives
        /// 
        /// Class imbalance e.g.:
        /// Foreground: 1–5% of total pixels; Background: 95–99%
        /// Therefore, in most segmentation cases ROC-AUC is overly optimistic !!!
        /// </summary>
        /// <param name="prediction">Model prediction tensor.</param>
        /// <param name="target">Ground truth mask tensor.</param>
        /// <param name="numThresholds">Granularity of thresholds .</param>
        /// <returns>auc</returns>
        public static float ComputeRocAndAuc(Tensor prediction, Tensor target, int numThresholds = 100)
        {

            var prob = prediction.sigmoid();
            var probFlat = prob.flatten().to_type(ScalarType.Float32);
            var targetFlat = target.flatten().to_type(ScalarType.Float32);

            // Sort probabilities in descending order
            var (values, indices) = sort(probFlat, descending: true);
            var sortedProbabilities = values;
            var sortedIndices = indices;
            var sortedTarget = targetFlat.index_select(0, indices);

            // True/false positive rate list
            var tprList = new List<float>();
            var fprList = new List<float>();

            // Total positives and negatives
            var totalPos = targetFlat.sum().ToSingle();
            var totalNeg = targetFlat.numel() - totalPos;

            // Avoid division by zero
            totalPos = Math.Max(totalPos, 1e-6f);
            totalNeg = Math.Max(totalNeg, 1e-6f);

            var numSamples = probFlat.shape[0];
            var step = Math.Max(1, numSamples / numThresholds);


            for (var i = 0; i < numSamples; i += (int)step)
            {
                var threshold = values[i].item<float>();
                var predictionBin = probFlat >= threshold;

                var tp = sum(predictionBin.logical_and(targetFlat == 1)).to_type(ScalarType.Float32);
                var fp = sum(predictionBin.logical_and(targetFlat == 0)).to_type(ScalarType.Float32);

                var tpr = tp.ToSingle() / totalPos;
                var fpr = fp.ToSingle() / totalNeg;

                tprList.Add(tpr);
                fprList.Add(fpr);
            }

            // Compute AUC using trapezoidal rule (approximate integral)
            var auc = 0f;
            for (var i = 1; i < fprList.Count; i++)
            {
                var dx = fprList[i] - fprList[i - 1];
                var avgY = (tprList[i] + tprList[i - 1]) / 2;
                auc += dx * avgY;
            }

            return auc;
        }

        #endregion

        #region Multiclass - Multilabel

        public static Tensor FocalLossMulticlass(
            Tensor logits,     // shape: [B, C, H, W] — raw logits
            Tensor targets,    // shape: [B, H, W] — class indices (int64)
            float gamma = 2.0f,
            float alpha = 1.0f,
            ScalarType dtype = ScalarType.Float32)
        {
            // Convert logits to float
            logits = logits.to_type(dtype);

            // Cross-entropy loss (no reduction)
            var ceLoss = functional.cross_entropy(
                logits,
                targets,
                reduction: Reduction.None
            ); // shape: [B, H, W]

            // Get softmax probabilities
            var probs = logits.softmax(1); // [B, C, H, W]

            // Gather p_t: prob of the true class
            var targetsLong = targets.unsqueeze(1);               // [B, 1, H, W]
            var pt = probs.gather(1, targetsLong).squeeze(1);     // [B, H, W]

            // Compute focal modulator
            var focalWeight = pow(1 - pt, gamma);

            // Final focal loss
            var loss = alpha * focalWeight * ceLoss;

            return loss.mean();
        }

        /// <summary>
        /// Dice loss + Cross-entropy loss for multi-label segmentation
        /// </summary>
        /// <param name="pred"></param>
        /// <param name="target"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public static Tensor DiceCrossEntropyLoss_MultiLabel(
            Tensor pred,       // [B, C, H, W] logits
            Tensor target,     // [B, C, H, W] binary masks
            float smooth = 1e-6f)
        {
            // Ensure float type
            target = target.to_type(ScalarType.Float32);

            // BCE with logits (per pixel, per class)
            var bce = functional.binary_cross_entropy_with_logits(pred, target);

            // Sigmoid on logits to get probs
            var prob = pred.sigmoid();  // [B, C, H, W]

            // Flatten for Dice
            var probFlat = prob.flatten(start_dim: 2);     // [B, C, H*W]
            var targetFlat = target.flatten(start_dim: 2); // [B, C, H*W]

            var intersection = (probFlat * targetFlat).sum(dim: 2);          // [B, C]
            var union = probFlat.sum(dim: 2) + targetFlat.sum(dim: 2);       // [B, C]

            var dice = (2 * intersection + smooth) / (union + smooth);       // [B, C]
            var diceLoss = 1 - dice.mean();  // scalar

            // 4. Combine losses
            return bce + diceLoss;
        }

        /// <summary>
        /// Dice loss + Cross-entropy loss for multi-class segmentation
        /// </summary>
        /// <param name="pred"></param>
        /// <param name="target"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public static Tensor DiceCrossEntropyLoss(Tensor pred, Tensor target, float smooth = 1e-6f)
        {
            // pred: [B, C, H, W] - raw logits
            // target: [B, H, W] - class indices

            var numClasses = pred.shape[1];

            // Compute cross-entropy loss (built-in)
            var ceLoss = functional.cross_entropy(pred, target);

            // Convert target to one-hot: [B, C, H, W]
            // Error if target has values outside [0, numClasses-1] usually in the masks
            var targetOneHot = functional.one_hot(target, numClasses) // shape: [B, H, W, C]
                .permute(0, 3, 1, 2) // to [B, C, H, W]
                .to_type(pred.dtype); // match dtype

            // Apply softmax to get class probabilities
            var probs = pred.softmax(1); // [B, C, H, W]

            // Flatten for Dice calculation: [B, C, H*W]
            var probsFlat = probs.flatten(start_dim: 2);
            var targetFlat = targetOneHot.flatten(start_dim: 2);

            // Compute per-class Dice
            var intersection = (probsFlat * targetFlat).sum(dim: 2);     // [B, C]
            var total = probsFlat.sum(dim: 2) + targetFlat.sum(dim: 2);  // [B, C]

            var dice = (2 * intersection + smooth) / (total + smooth);   // [B, C]
            var diceLoss = 1 - dice.mean();  // scalar

            // Combine losses
            return ceLoss + diceLoss;
        }

        #endregion

    }
}
