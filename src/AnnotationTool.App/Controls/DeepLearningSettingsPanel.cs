using AnnotationTool.Core.Models;
using AnnotationTool.Core.Services;

namespace AnnotationTool.App.Controls
{

	public partial class DeepLearningSettingsPanel : UserControl
	{
		public event EventHandler SettingsChanged;
		private IProjectPresenter presenter;

		public DeepLearningSettingsPanel()
		{
			InitializeComponent();

			// When the panel's handle is created, we can safely tweak layout if needed
			this.HandleCreated += (s, e) =>
			{
				if (pgDeepLearningSettings.SelectedObject != null)
				{
					ExpandAllGridItems();
					AdjustPropertyGridSplitter();
					SetPropertyGridDescriptionHeight(24);
				}
			};
		}

		// For constructor chaining because parameterless constructor is required for designer.
		public DeepLearningSettingsPanel(IProjectPresenter presenter) : this()
		{
			Initialize(presenter);
		}

		public void Initialize(IProjectPresenter presenter)
		{
			this.presenter = presenter;
			pgDeepLearningSettings.PropertyValueChanged += PgDeepLearningSettings_PropertyValueChanged;
			RefreshBindings();
			pgDeepLearningSettings.ExpandAllGridItems();
		}

        public bool ForceCpuOnly { get; set; } = false;
        //public bool ForceModelComplexity{ get; set; } = false;

        private void PgDeepLearningSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
            if (ForceCpuOnly && e.ChangedItem.PropertyDescriptor.Name == "Device")
            {
                presenter.Project.Settings.TrainModelSettings.Device = ComputeDevice.Cpu;

                // Snap UI back
                pgDeepLearningSettings.Refresh();

				MessageBox.Show("GPU acceleration is disabled on this system because CUDA GPU is not available.", "Cuda not abailable", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return; // do not propagate change to SettingsChanged
            }
            //if (ForceModelComplexity && e.ChangedItem.PropertyDescriptor.Name == "ModelComplexity")
            //{
            //    presenter.Project.Settings.TrainModelSettings.ModelComplexity = ModelComplexity.Medium;

            //    // Snap UI back
            //    pgDeepLearningSettings.Refresh();

            //    MessageBox.Show("Feature not implemented yet", "Feature model complexity", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    return; // do not propagate change to SettingsChanged
            //}

            SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshBindings()
		{
			if (presenter?.Project?.Settings == null)
			{
				pgDeepLearningSettings.SelectedObject = null;
				return;
			}

			pgDeepLearningSettings.SelectedObject = presenter.Project.Settings;
			pgDeepLearningSettings.Refresh();

			if (this.IsHandleCreated)
			{
				ExpandAllGridItems();
				AdjustPropertyGridSplitter();
				SetPropertyGridDescriptionHeight(240);
			}
			else
			{
				// If handle isn't created yet, defer once
				void handler(object? s, EventArgs e)
				{
					this.HandleCreated -= handler;

					if (pgDeepLearningSettings.SelectedObject != null)
					{
						ExpandAllGridItems();
						AdjustPropertyGridSplitter();
						SetPropertyGridDescriptionHeight(240);
					}
				}

				this.HandleCreated += handler;
			}
		}

		/// <summary>
		/// Expands all categories/items in the PropertyGrid.
		/// </summary>
		private void ExpandAllGridItems()
		{
			var root = pgDeepLearningSettings.SelectedGridItem;
			if (root == null)
				return;

			foreach (GridItem child in root.GridItems)
			{
				ExpandRecursive(child);
			}
		}

		private void ExpandRecursive(GridItem item)
		{
			try { item.Expanded = true; } catch { }

			foreach (GridItem child in item.GridItems)
			{
				ExpandRecursive(child);
			}
		}

		private void AdjustPropertyGridSplitter()
		{
			if (!this.IsHandleCreated)
				return;

			this.BeginInvoke(new Action(() =>
			{
				var gridView = pgDeepLearningSettings.Controls
					.Cast<Control>()
					.FirstOrDefault(c => c.GetType().Name == "PropertyGridView");

				if (gridView == null)
					return;

				int desiredPosition = 220;

				var moveSplitter = gridView.GetType().GetMethod(
					"MoveSplitterTo",
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic);

				moveSplitter?.Invoke(gridView, new object[] { desiredPosition });
			}));
		}

		/// <summary>
		/// Changes the height of the help/description box at the bottom.
		/// // ToDo: This seems to not work as expected. :(
		/// </summary>
		private void SetPropertyGridDescriptionHeight(int height)
		{
			if (!this.IsHandleCreated)
				return;

			this.BeginInvoke(new Action(() =>
			{
				var doc = pgDeepLearningSettings.Controls
				.Cast<Control>()
				.FirstOrDefault(c => c.GetType().Name == "DocComment");

			if (doc == null)
				return;

			var method = doc.GetType().GetMethod(
				"SetCommentHeight",
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.NonPublic);

			method?.Invoke(doc, new object[] { height });
			}));
		}
	}

}
