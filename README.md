# Deep Learning Editor – End-to-end Image Segmentation

AnnotationTool is a C# WinForms application for the **complete annotation → training → inference** loop focused on **industrial image segmentation** tasks with typically **small datasets**.  

It provides a simple workflow for generating pixel-level annotations, training segmentation models using TorchSharp, and running inference with visualization and metrics.


### ⚠️ **Project Status: Work in Progress (WIP)**  
The current version focuses on **binary segmentation**. 
A number of enhancements are planned – see **TODO / Roadmap** at the bottom.

### ⚠️⚠️⚠️ IMPORTANT 
Calling native cuda function empty_cache() works only with TorchSharp v0.105.1 atm (since dll is build from libtorch 2.5.1.0 and cuda 12.1 )

### 📦 Included Sample Project

This repository includes a **ready-to-use sample project** so you can immediately try out the workflow.

<img width="1680" height="871" alt="GUI_DeepLearningEditor" src="https://github.com/user-attachments/assets/d14575a3-f028-4e6a-b562-b1655af156ed" />


---

## Key Features

### 🖼️ Project-based workflow
Each project stores:
- Images
- Training settings in a json file
- Structured folder layout (`Images/`, `Annotations/`, `Masks/`, `Results/`, `Logs/`)

### ✏️ Annotation UI
- Brush & eraser with adjustable size
- ROI definition with movable & resizable handles
- Per-image dataset split controls
- Feature palette
- Zoom & pan

### 🧮 Preprocessing
- Optional grayscale conversion
- Downsampling & patch slicing
- Photometric and geometric augmentations

### 🤖 Model Training
- TorchSharp UNet implementation
- CUDA or CPU support
- Automatic batch size estimation
- Early stopping settings
- Live training charts

### 🔍 Inference
- Patch-based inference
- Prediction heatmaps + overlays
- Aggregated (macro/micro) metrics:
  - Dice, IoU, Precision, Recall, Accuracy, FPR

---

## Typical Workflow

1. Create a project  
2. Add images  
3. Add one feature (foreground)  
4. Annotate using brush/eraser and ROI  
5. Configure preprocessing, augmentations, and training settings  
6. Train 
7. Run inference  
8. Inspect metrics & heatmaps

---

## 🚧 Work in Progress / Roadmap

This project is actively developed. The following improvements are planned or in progress:


### 🔧 Foundations
- Upgrade all class libraries (`AnnotationTool.Ai`, `AnnotationTool.Core`) to **.NET 10**  
  → Allows BFloat16 support, performance boosts, (unsafe pointers → Span/Memory)

### 🎨 Annotation
- Multiclass segmentation support (multiple features/classes)

### 🧮 Preprocessing & Augmentation
- Augmentation preview window (before training)  
- Advanced augmentations (elastic, cutout, gamma, random crop)
- Synthetic sample generation for industrial datasets

### 🤖 Models & Training
- Multiclass UNet training pipeline
- BF16 after .NET 10 upgrade
- Model zoo: Residual UNet, UNet++, Attention UNet, Mobile-UNet
- Auto LR finder

### 📈 Metrics & Visualization
- PR/ROC curves
- Confusion matrix
- Slice-level metrics (useful for defect inspection tasks)

### 🔍 Inference & Deployment
- Batch inference for external folders

### 🔬 Advanced Ideas
- Active learning loop (model-guided refinement)
- Semi-supervised learning options


## Project Structure

At repo level:

```text
DeepLearningEditor.sln
src/
  AnnotationTool.App/      # WinForms UI: annotation, training, inference
  AnnotationTool.Ai/       # TorchSharp models, training and inference pipeline
  AnnotationTool.Core/     # Core models, services, configuration, utilities

---

## Requirements

- Windows 10/11  
- .NET 10 (WinForms App already targets .NET 10)
- TorchSharp (CPU or CUDA backend)
- Visual Studio 2022+
- Optional: NVIDIA GPU with compatible CUDA runtime

---
