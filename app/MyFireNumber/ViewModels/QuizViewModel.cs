using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public partial class QuizChoice(string value, string label, string description) : ObservableObject
{
    public string Value { get; } = value;
    public string Label { get; } = label;
    public string Description { get; } = description;

    [ObservableProperty]
    private bool isSelected;
}

public partial class QuizViewModel : ObservableObject
{
    private const int QuestionCount = 8;
    private readonly ICalculatorCatalog catalog;
    private readonly IConfirmationService confirmationService;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly IDraftRepository draftRepository;
    private readonly INavigationService navigationService;
    private readonly IOnboardingService onboardingService;
    private readonly IRecentActivityRepository recentActivityRepository;
    private FireQuizRecommendation? recommendation;
    private int? currentAge;
    private int? retirementAge;
    private double? currentSavings;
    private double? annualIncome;
    private double? annualExpenses;
    private FireQuizLifestyle? lifestyle;
    private FireQuizWorkPreference? workPreference;
    private FireQuizPrimaryGoal? primaryGoal;

    public QuizViewModel(
        ICalculatorCatalog catalog,
        IConfirmationService confirmationService,
        ICalculatorDefaultsService calculatorDefaultsService,
        IDraftRepository draftRepository,
        INavigationService navigationService,
        IOnboardingService onboardingService,
        IRecentActivityRepository recentActivityRepository)
    {
        this.catalog = catalog;
        this.confirmationService = confirmationService;
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.draftRepository = draftRepository;
        this.navigationService = navigationService;
        this.onboardingService = onboardingService;
        this.recentActivityRepository = recentActivityRepository;

        var defaults = calculatorDefaultsService.Current;
        currentAge = defaults.CurrentAge;
        retirementAge = defaults.RetirementAge;
        ShowQuestion();
    }

    public ObservableCollection<QuizChoice> Choices { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuestionProgress), nameof(QuestionProgressText), nameof(CanGoBack))]
    private int questionIndex;

    [ObservableProperty]
    private string questionTitle = string.Empty;

    [ObservableProperty]
    private string questionSubtitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinue))]
    private string answerText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNumericQuestion))]
    private bool isChoiceQuestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQuestionVisible))]
    private bool isRecommendationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private string recommendationTitle = string.Empty;

    [ObservableProperty]
    private string recommendationReason = string.Empty;

    [ObservableProperty]
    private string recommendationDescription = string.Empty;

    [ObservableProperty]
    private string recommendationBenefits = string.Empty;

    [ObservableProperty]
    private string recommendationIconGlyph = "\uf201";

    public double QuestionProgress => (QuestionIndex + 1d) / QuestionCount;
    public string QuestionProgressText => $"Question {QuestionIndex + 1} of {QuestionCount}";
    public bool CanGoBack => QuestionIndex > 0;
    public bool IsQuestionVisible => !IsRecommendationVisible;
    public bool IsNumericQuestion => !IsChoiceQuestion;
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool CanContinue => IsChoiceQuestion
        ? Choices.Any(choice => choice.IsSelected)
        : !string.IsNullOrWhiteSpace(AnswerText);

    partial void OnAnswerTextChanged(string value)
    {
        ValidationMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        onboardingService.Complete();
        await navigationService.GoToAsync("//home");
    }

    [RelayCommand]
    private void SelectChoice(QuizChoice choice)
    {
        foreach (var option in Choices)
        {
            option.IsSelected = option == choice;
        }

        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(CanContinue));
    }

    [RelayCommand]
    private void Previous()
    {
        if (QuestionIndex == 0)
        {
            return;
        }

        _ = SaveCurrentAnswer();
        QuestionIndex--;
        ShowQuestion();
    }

    [RelayCommand]
    private void Next()
    {
        if (!SaveCurrentAnswer())
        {
            return;
        }

        if (QuestionIndex < QuestionCount - 1)
        {
            QuestionIndex++;
            ShowQuestion();
            return;
        }

        ShowRecommendation();
    }

    [RelayCommand]
    private void StartOver()
    {
        currentAge = null;
        retirementAge = null;
        currentSavings = null;
        annualIncome = null;
        annualExpenses = null;
        lifestyle = null;
        workPreference = null;
        primaryGoal = null;
        recommendation = null;
        IsRecommendationVisible = false;
        QuestionIndex = 0;
        ShowQuestion();
    }

    [RelayCommand]
    private async Task UseRecommendationAsync()
    {
        if (recommendation is null)
        {
            return;
        }

        var existingDraft = await draftRepository.GetAsync(recommendation.CalculatorId);
        if (existingDraft is not null)
        {
            var replace = await confirmationService.ConfirmAsync(
                "Replace existing draft?",
                $"Your current {recommendation.Title} draft will be replaced with these quiz answers.",
                "Replace",
                "Cancel");
            if (!replace)
            {
                return;
            }
        }

        var draft = CreateRecommendedDraft(recommendation.CalculatorId);
        await draftRepository.SaveAsync(draft);
        onboardingService.Complete();
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            recommendation.CalculatorId,
            DateTime.UtcNow));
        await navigationService.GoToAsync(
            $"../calculator?calculatorId={Uri.EscapeDataString(recommendation.CalculatorId)}");
    }

    private bool SaveCurrentAnswer()
    {
        ValidationMessage = string.Empty;
        if (IsChoiceQuestion)
        {
            var selected = Choices.FirstOrDefault(choice => choice.IsSelected);
            if (selected is null)
            {
                ValidationMessage = "Choose an option to continue.";
                return false;
            }

            SaveChoiceAnswer();
            return true;
        }

        if (!double.TryParse(
                AnswerText,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.CurrentCulture,
                out var value))
        {
            ValidationMessage = "Enter a valid number to continue.";
            return false;
        }

        switch (QuestionIndex)
        {
            case 0 when value is >= 18 and <= 80:
                currentAge = (int)value;
                break;
            case 1 when currentAge.HasValue && value > currentAge.Value && value <= 80:
                retirementAge = (int)value;
                break;
            case 2 when value >= 0:
                currentSavings = value;
                break;
            case 3 when value >= 0:
                annualIncome = value;
                break;
            case 4 when value >= 0:
                annualExpenses = value;
                break;
            default:
                ValidationMessage = QuestionIndex switch
                {
                    0 => "Enter an age from 18 to 80.",
                    1 => $"Enter a target age after {currentAge ?? 18} and no later than 80.",
                    _ => "Enter an amount of zero or more."
                };
                return false;
        }

        return true;
    }

    private void SaveChoiceAnswer()
    {
        var value = Choices.FirstOrDefault(choice => choice.IsSelected)?.Value;
        if (value is null)
        {
            return;
        }

        switch (QuestionIndex)
        {
            case 5:
                lifestyle = Enum.Parse<FireQuizLifestyle>(value);
                break;
            case 6:
                workPreference = Enum.Parse<FireQuizWorkPreference>(value);
                break;
            case 7:
                primaryGoal = Enum.Parse<FireQuizPrimaryGoal>(value);
                break;
        }
    }

    private void ShowQuestion()
    {
        IsRecommendationVisible = false;
        ValidationMessage = string.Empty;
        Choices.Clear();
        IsChoiceQuestion = QuestionIndex >= 5;

        (QuestionTitle, QuestionSubtitle, AnswerText) = QuestionIndex switch
        {
            0 => ("How old are you?", "This helps establish your planning timeline.", FormatAnswer(currentAge)),
            1 => ("When do you want to reach financial independence?", "Choose a target age after your current age.", FormatAnswer(retirementAge)),
            2 => ("How much do you currently have saved or invested?", "Include retirement accounts and other invested assets.", FormatAnswer(currentSavings)),
            3 => ("What is your annual household income?", "Use your current annual income before taxes.", FormatAnswer(annualIncome)),
            4 => ("What are your annual expenses?", "Use current spending or your expected retirement spending.", FormatAnswer(annualExpenses)),
            5 => ("What lifestyle do you want in retirement?", "Choose the spending style that best matches your goal.", string.Empty),
            6 => ("What is your ideal work situation after reaching FI?", "Choose how you want work to fit into your life.", string.Empty),
            _ => ("What is most important to you?", "Choose your primary motivation for financial independence.", string.Empty)
        };

        AddChoicesForCurrentQuestion();
        OnPropertyChanged(nameof(QuestionProgress));
        OnPropertyChanged(nameof(QuestionProgressText));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanContinue));
    }

    private void AddChoicesForCurrentQuestion()
    {
        IEnumerable<QuizChoice> choices = QuestionIndex switch
        {
            5 =>
            [
                new(nameof(FireQuizLifestyle.Minimal), "Minimal / Frugal", "Living simply on about $30,000-$40,000 per year."),
                new(nameof(FireQuizLifestyle.Moderate), "Moderate", "Comfortable basics on about $40,000-$70,000 per year."),
                new(nameof(FireQuizLifestyle.Comfortable), "Comfortable", "Few major sacrifices on about $70,000-$100,000 per year."),
                new(nameof(FireQuizLifestyle.Luxury), "Luxury / Fat", "A high-end lifestyle of $100,000 or more per year.")
            ],
            6 =>
            [
                new(nameof(FireQuizWorkPreference.QuitCompletely), "Quit completely", "Leave paid work behind."),
                new(nameof(FireQuizWorkPreference.PartTime), "Part-time work", "Work for benefits, purpose, or extra income."),
                new(nameof(FireQuizWorkPreference.Coast), "Coast mode", "Choose lower-stress work that covers current expenses."),
                new(nameof(FireQuizWorkPreference.Flexible), "Stay flexible", "Keep multiple work and lifestyle options open.")
            ],
            7 =>
            [
                new(nameof(FireQuizPrimaryGoal.RetireEarly), "Retire as soon as possible", "Prioritize leaving full-time work early."),
                new(nameof(FireQuizPrimaryGoal.FinancialSecurity), "Financial security", "Build peace of mind and resilience."),
                new(nameof(FireQuizPrimaryGoal.MaintainLifestyle), "Maintain my lifestyle", "Retire without a large spending change."),
                new(nameof(FireQuizPrimaryGoal.Flexibility), "Flexibility", "Create more options and work-life balance.")
            ],
            _ => []
        };

        var selectedValue = QuestionIndex switch
        {
            5 => lifestyle?.ToString(),
            6 => workPreference?.ToString(),
            7 => primaryGoal?.ToString(),
            _ => null
        };
        foreach (var choice in choices)
        {
            choice.IsSelected = choice.Value == selectedValue;
            Choices.Add(choice);
        }
    }

    private void ShowRecommendation()
    {
        var answers = new FireQuizAnswers(
            currentAge!.Value,
            retirementAge!.Value,
            currentSavings!.Value,
            annualIncome!.Value,
            annualExpenses!.Value,
            lifestyle!.Value,
            workPreference!.Value,
            primaryGoal!.Value);
        recommendation = FireQuizRecommender.Recommend(answers);
        RecommendationTitle = recommendation.Title;
        RecommendationReason = recommendation.Reason;
        RecommendationDescription = recommendation.Description;
        RecommendationBenefits = string.Join(Environment.NewLine, recommendation.Benefits.Select(benefit => $"- {benefit}"));
        RecommendationIconGlyph = catalog.GetRequired(recommendation.CalculatorId).IconGlyph;
        onboardingService.SetRecommendation(recommendation.CalculatorId);
        IsRecommendationVisible = true;
    }

    private DraftRecord CreateRecommendedDraft(string calculatorId)
    {
        var age = currentAge!.Value;
        var targetAge = retirementAge!.Value;
        var savings = currentSavings!.Value;
        var income = annualIncome!.Value;
        var expenses = annualExpenses!.Value;
        var contribution = Math.Max(0, income - expenses);
        object payload = calculatorId switch
        {
            "lean-fire" => LeanFireDraft.Default with { CurrentAge = age, RetirementAge = targetAge, CurrentSavings = savings, AnnualContribution = contribution, AnnualIncome = income, AnnualExpenses = expenses },
            "fat-fire" => FatFireDraft.Default with { CurrentAge = age, RetirementAge = targetAge, CurrentSavings = savings, AnnualContribution = contribution, AnnualIncome = income, AnnualExpenses = expenses },
            "barista-fire" => BaristaFireDraft.Default with { CurrentAge = age, CurrentSavings = savings, AnnualContribution = contribution, AnnualExpenses = expenses },
            "coast-fire" => CoastFireDraft.Default with { CurrentAge = age, RetirementAge = targetAge, CurrentSavings = savings, AnnualContribution = contribution, AnnualExpenses = expenses },
            "reverse-fire" => ReverseFireDraft.Default with { CurrentAge = age, TargetRetirementAge = targetAge, CurrentSavings = savings, AnnualExpenses = expenses },
            "savings-rate" => SavingsInvestmentDraft.Default with { StartingAmount = savings, ContributionAmount = contribution / 12, YearsInvesting = targetAge - age, AnnualIncome = income, CurrentAge = age },
            _ => StandardFireDraft.Default with { CurrentAge = age, RetirementAge = targetAge, CurrentSavings = savings, AnnualContribution = contribution, AnnualIncome = income, AnnualExpenses = expenses }
        };

        return new DraftRecord(
            calculatorId,
            1,
            JsonSerializer.Serialize(payload, payload.GetType()),
            DateTime.UtcNow);
    }

    private static string FormatAnswer(double? value)
    {
        return value?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
