# Deep Learning Editor – End-to-end Image Segmentation

AnnotationTool is a Windows desktop application for end-to-end image segmentation, covering the full workflow:

Annotation → Training → Inference → Visualization

It is designed for industrial and research use cases where datasets are small-to-medium sized, labeling accuracy matters, and users need direct visual feedback rather than opaque batch pipelines.

The application is built with WinForms (C#) and TorchSharp, and supports both binary and multiclass segmentation.


### **Project Status**  
🟢 Active development – stable core

- Binary and multiclass segmentation fully supported
- End-to-end workflow is functional and tested
- Geometry, preprocessing, and inference pipelines are consistent and shared
- UI is responsive and optimized for interactive annotation

The current version is already usable for real projects, with continued improvements planned.


### 📦 Included Sample Project

This repository includes a **ready-to-use sample project** so you can immediately try out the workflow.

<img width="1680" height="871" alt="GUI_DeepLearningEditor" src="https://github.com/user-attachments/assets/3779a494-fb1a-409d-9284-a13131d05850" />


---

## Key Features

### 🖼️ Project-based workflow
Each project stores:
- Images
- Training settings in a json file
- Structured folder layout (`Images/`, `Annotations/`, `Masks/`, `Results/`, `Logs/`)

### ✏️ Annotation UI
- Brush & eraser with adjustable size
- Feature-based labeling
- ROI definition with movable & resizable handles
- Per-image dataset split controls
- Feature palette with color overlay
- Zoom & pan

### 🧮 Preprocessing
- Optional grayscale conversion
- Downsampling & patch slicing
- Shared preprocessing pipeline for training and inference
- Photometric and geometric augmentations

### 🤖 Model Training
- TorchSharp UNet implementation
- Binary and multiclass segmentation
- CUDA or CPU support
- Automatic batch size estimation
- Early stopping settings
- Live training charts

### 🔍 Inference
- Patch-based inference
- Full-resolution reconstruction
- Probability heatmaps
- Aggregated (macro/micro) metrics:
  - Dice, IoU, Precision, Recall, Accuracy, FPR

---

## Typical Workflow

1. Create a project  
2. Add images  
3. Add features
4. Annotate using brush/eraser and ROI  
5. Configure preprocessing, augmentations, and training settings  
6. Train 
7. Run inference  
8. Inspect metrics & heatmaps

---

### 🧩 Inference SDK

In addition to the desktop application, the project provides a headless inference SDK that allows you to run trained models in other .NET applications (.net framework 4.8/.net standard 2.0/.NET).

#### Basic SDK Usage

```csharp
using AnnotationTool.InferenceSdk;
using OpenCvSharp;

using var session = SegmentationInferenceSession.Load(
    modelPath: "model.bin",
    settingsJsonPath: "settings.json",
    device: InferenceDevice.Cpu // or Cuda
);

Mat image = Cv2.ImRead("image.png");

// Full image inference
var result = session.Run(image);

// Optional ROI inference
var roi = new Rect(100, 50, 512, 512);
var roiResult = session.Run(image, roi);
```
#### SDK Output

```csharp
IReadOnlyDictionary<int, Mat>
```
- Key = classId (1..N, background is implicit)
- Value = full-resolution probability mask for that class
- Returned Mat objects must be disposed by the caller


---
## 🚧 Work in Progress / Roadmap

This project is actively developed. The following improvements are planned or in progress:

### 🧮 Preprocessing & Augmentation
- Augmentation preview window (before training)  
- Advanced augmentations (elastic, cutout, gamma, random crop)
- Synthetic sample generation for industrial datasets

### 🤖 Models & Training
- Model zoo: Residual UNet, UNet++, Attention UNet, Mobile-UNet
- Auto LR finder

### 📈 Metrics & Visualization
- PR/ROC curves
- Confusion matrix
- Slice-level metrics (useful for defect inspection tasks)

### 🔍 Inference & Deployment
- Batch inference for external folders
- Extend SDK batch helpers

### 🔬 Advanced Ideas
- Active learning loop (model-guided refinement)
- Semi-supervised learning options


## Project Structure

At repo level:

```text
DeepLearningEditor.sln
src/
  AnnotationTool.App/          # WinForms UI: annotation, training, inference
  AnnotationTool.Ai/           # TorchSharp models, training and inference pipeline
  AnnotationTool.Core/         # Core models, services, configuration, utilities
  AnnotationTool.InferenceSdk/ # Headless inference SDK
---

## Requirements

- Windows 10/11  
- .NET 10 (WinForms App targets .NET 10, Class libs target .net Standard 2.0 for legacy reasons)
- TorchSharp (CPU or CUDA backend)
- Visual Studio 2022+
- Optional: NVIDIA GPU with compatible CUDA runtime

```
