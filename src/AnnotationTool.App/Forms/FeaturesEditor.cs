using AnnotationTool.Core.Models;
using static AnnotationTool.Core.Utils.CoreUtils;

namespace AnnotationTool.App.Forms
{
	public partial class FeaturesEditor : Form
    {
		
		private List<Feature>? features;


		public FeaturesEditor(List<Feature> currentFeatures)
		{
			InitializeComponent();
			SetupUi(currentFeatures);
		}
								
		private void SetupUi(List<Feature> currentFeatures)
		{
			features = new List<Feature>(currentFeatures);

			lbTrainFeatures.Items.AddRange(features.ToArray());
		}

		public List<Feature> GetUpdatedFeatures()
		{
			return features;
		}

		private void lBTrainFeatures_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index < 0) 
				return;

			e.DrawBackground();

			var f = (Feature)lbTrainFeatures.Items[e.Index];
			
			var textColor = GetContrastTextColor(Color.FromArgb(f.Argb));

			using (Brush backBrush = new SolidBrush(Color.FromArgb(f.Argb)))
			{
				e.Graphics.FillRectangle(backBrush, e.Bounds);
			}

			using (Brush textBrush = new SolidBrush(textColor))
			{
				e.Graphics.DrawString(f.Name, e.Font, textBrush, e.Bounds.X + 1, e.Bounds.Y + 1);
			}
			
			e.DrawFocusRectangle();
		}

		private static string ShowInputBox(string prompt, string title, string defaultValue = "")
		{
			using var form = new Form { Width = 300, Height = 150, Text = title };
			
			var label = new Label { Text = prompt, Left = 10, Top = 20, Width = 280 };
			var textBox = new TextBox { Left = 10, Top = 40, Width = 280, Text = defaultValue };
			var ok = new Button { Text = "OK", Left = 100, Top = 70, Width = 80, DialogResult = DialogResult.OK, ForeColor = Color.Black};
			var cancel = new Button { Text = "Cancel", Left = 190, Top = 70, Width = 80, DialogResult = DialogResult.Cancel, ForeColor = Color.Black };

			form.Controls.Add(label);
			form.Controls.Add(textBox);
			form.Controls.Add(ok);
			form.Controls.Add(cancel);

			form.AcceptButton = ok;
			form.CancelButton = cancel;

			if (form.ShowDialog() == DialogResult.OK)
				return textBox.Text;

			return null;
		}

		#region Buttons

		private void btnAdd_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				if (this.features.Count == 1)
				{
					MessageBox.Show("Model supports only one feature at the moment. Sorry :(", "Only one feature", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				var name = Invoke((Func<string>)(() => ShowInputBox("Enter feature name:", "Add Feature")));
				if (string.IsNullOrEmpty(name)) return;

				if (features.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
				{
					Invoke((Action)(() => MessageBox.Show($"A feature named '{name}' already exists.", "Duplicate Feature",
						MessageBoxButtons.OK, MessageBoxIcon.Warning)));
					return;
				}

				var cd = new ColorDialog();
				var result = Invoke((Func<DialogResult>)(() => cd.ShowDialog()));
				if (result == DialogResult.OK)
				{
					var f = new Feature { Name = name, Argb = cd.Color.ToArgb() };
					Invoke((Action)(() =>
					{
						features.Add(f);
						lbTrainFeatures.Items.Add(f);
					}));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error adding feature: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;
			try
			{
				if (lbTrainFeatures.SelectedItem != null)
				{
					var f = (Feature)lbTrainFeatures.SelectedItem;
					features!.Remove(f);
					lbTrainFeatures.Items.Remove(f);
				}
				else
				{
					MessageBox.Show("No feature selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error removing feature: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		private void btnRename_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				if (lbTrainFeatures.SelectedItem != null)
				{
					var f = (Feature)lbTrainFeatures.SelectedItem;
					var newName = ShowInputBox("Enter new name:", "Rename Feature", f.Name);
					if (!string.IsNullOrEmpty(newName))
					{
						if (features!.Any(feat => feat != f && feat.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
						{
							MessageBox.Show($"A feature named '{newName}' already exists.", "Duplicate Feature",
								MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}
						f.Name = newName;
						lbTrainFeatures.Invalidate();
					}
				}
				else
				{
					MessageBox.Show("No feature selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error renaming feature: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		private void btnChangeColor_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				if (lbTrainFeatures.SelectedItem != null)
				{
					var f = (Feature)lbTrainFeatures.SelectedItem;
					var cd = new ColorDialog { Color = Color.FromArgb( f.Argb) };
					if (cd.ShowDialog() == DialogResult.OK)
					{
						f.Argb = cd.Color.ToArgb();
						lbTrainFeatures.Invalidate();
					}
				}
				else
				{
					MessageBox.Show("No feature selected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error changing feature color: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			(sender as Button)!.Enabled = false;

			try
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error adding feature color: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				(sender as Button)!.Enabled = true;
			}
		}


		#endregion

	}
}
