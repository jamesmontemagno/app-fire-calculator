using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class QuizPage : ContentPage
{
    public QuizPage(QuizViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}