using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    /// <summary>
    /// Interaction logic for NameDIalog.xaml
    /// </summary>
    public partial class QuestionDialog : Window
    {

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="question"> Displayed question </param>
		/// <param name="defaultAnswer"> Default answer </param>
		public QuestionDialog(string question, string defaultAnswer)
		{
			InitializeComponent();
			lblQuestion.Content = question;
			txtAnswer.Text = defaultAnswer;
		}

		/// <summary>
		/// Filter name
		/// - only a-zA-Z0-9_- allowed
		/// </summary>
		private void FilterName(object sender, TextChangedEventArgs args)
		{
			txtAnswer.Text = Regex.Replace(txtAnswer.Text, "[^a-zA-Z0-9_-]+", "", RegexOptions.Compiled);
			txtAnswer.CaretIndex = txtAnswer.Text.Length;
		} 

		/// <summary>
		/// On OK button clicked
		/// </summary>
		private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			if (txtAnswer.Text.Trim().Length != 0)
				this.DialogResult = true;
		}

		/// <summary>
		/// On window content rendered
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_ContentRendered(object sender, EventArgs e)
		{
			txtAnswer.SelectAll();
			txtAnswer.Focus();
		}

		/// <summary> Entered answer </summary>
		public string Answer
		{
			get { return txtAnswer.Text; }
		}

    }
}
