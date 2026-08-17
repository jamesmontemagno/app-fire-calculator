using System.Collections.ObjectModel;
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
    public string DisplayText => $"{Label}{Environment.NewLine}{Description}";
    public string AutomationId => $"QuizChoice{Value}";

    [ObservableProperty]
    private bool isSelected;
}

public sealed record QuizRecommendationOption(
    string CalculatorId,
    string Title,
    string Reason,
    string Description,
    string Benefits,
    string IconGlyph)
{
    public string AutomationId => $"QuizRecommendation{CalculatorId}";
}

public partial class QuizViewModel : ObservableObject
{
    private const int QuestionCount = 4;
    private readonly ICalculatorCatalog catalog;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly IConfirmationService confirmationService;
    private readonly IDraftRepository draftRepository;
    private readonly INavigationService navigationService;
    private readonly IOnboardingService onboardingService;
    private readonly IRecentActivityRepository recentActivityRepository;
    private FireQuizLifestyle? lifestyle;
    private FireQuizWorkPreference? workPreference;
    private FireQuizTimeline? timeline;
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
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.confirmationService = confirmationService;
        this.draftRepository = draftRepository;
        this.navigationService = navigationService;
        this.onboardingService = onboardingService;
        this.recentActivityRepository = recentActivityRepository;

        ShowQuestion();
    }

    public ObservableCollection<QuizChoice> Choices { get; } = [];
    public ObservableCollection<QuizRecommendationOption> AlternativeRecommendations { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(QuestionProgress),
        nameof(QuestionProgressText),
        nameof(CanGoBack),
        nameof(CanSkipQuestion))]
    private int questionIndex;

    [ObservableProperty]
    private string questionTitle = string.Empty;

    [ObservableProperty]
    private string questionSubtitle = string.Empty;

    public bool IsChoiceQuestion => true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQuestionVisible))]
    private bool isRecommendationVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private QuizRecommendationOption? primaryRecommendation;

    [ObservableProperty]
    private string recommendationConfidenceText = string.Empty;

    public double QuestionProgress => (QuestionIndex + 1d) / QuestionCount;
    public string QuestionProgressText => $"Question {QuestionIndex + 1} of {QuestionCount}";

    public bool CanGoBack => QuestionIndex > 0;
    public bool IsQuestionVisible => !IsRecommendationVisible;
    public bool IsNumericQuestion => false;
    public bool CanSkipQuestion => false;
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool CanContinue => Choices.Any(choice => choice.IsSelected);

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
        lifestyle = null;
        workPreference = null;
        timeline = null;
        primaryGoal = null;
        PrimaryRecommendation = null;
        AlternativeRecommendations.Clear();
        IsRecommendationVisible = false;
        QuestionIndex = 0;
        ShowQuestion();
    }

    [RelayCommand]
    private async Task UseRecommendationAsync(QuizRecommendationOption option)
    {
        var existingDraft = await draftRepository.GetAsync(option.CalculatorId);
        if (existingDraft is not null)
        {
            var replace = await confirmationService.ConfirmAsync(
                "Replace existing draft?",
                $"Your current {option.Title} draft will be replaced with the quiz details you supplied.",
                "Replace",
                "Cancel");
            if (!replace)
            {
                return;
            }
        }

        var draft = CreateRecommendedDraft(option.CalculatorId);
        await draftRepository.SaveAsync(draft);
        onboardingService.SetRecommendation(option.CalculatorId);
        onboardingService.Complete();
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            option.CalculatorId,
            DateTime.UtcNow));
        await navigationService.GoToAsync("//home");
        await navigationService.GoToAsync(
            CalculatorRoutes.Build(option.CalculatorId, returnHomeAfterSave: true));
    }

    private bool SaveCurrentAnswer()
    {
        ValidationMessage = string.Empty;
        var selected = Choices.FirstOrDefault(choice => choice.IsSelected);
        if (selected is null)
        {
            ValidationMessage = "Choose an option to continue.";
            return false;
        }

        SaveChoiceAnswer(selected.Value);
        return true;
    }

    private void SaveChoiceAnswer(string value)
    {
        switch (QuestionIndex)
        {
            case 0:
                lifestyle = Enum.Parse<FireQuizLifestyle>(value);
                break;
            case 1:
                workPreference = Enum.Parse<FireQuizWorkPreference>(value);
                break;
            case 2:
                timeline = Enum.Parse<FireQuizTimeline>(value);
                break;
            case 3:
                primaryGoal = Enum.Parse<FireQuizPrimaryGoal>(value);
                break;
        }
    }

    private void ShowQuestion()
    {
        IsRecommendationVisible = false;
        ValidationMessage = string.Empty;
        Choices.Clear();

        (QuestionTitle, QuestionSubtitle) = QuestionIndex switch
        {
            0 => ("What kind of retirement lifestyle are you planning for?", "Choose the closest fit. You can refine the numbers later."),
            1 => ("How would you like work to fit into your future?", "Financial independence can mean stopping, scaling back, or simply gaining options."),
            2 => ("How soon would you like to reach financial independence?", "A range is enough. This shapes which strategy is most useful to explore first."),
            _ => ("What matters most in your FIRE plan?", "Pick the priority you would protect when tradeoffs appear.")
        };

        AddChoicesForCurrentQuestion();
        OnPropertyChanged(nameof(QuestionProgress));
        OnPropertyChanged(nameof(QuestionProgressText));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanSkipQuestion));
        OnPropertyChanged(nameof(CanContinue));
    }

    private void AddChoicesForCurrentQuestion()
    {
        IEnumerable<QuizChoice> choices = QuestionIndex switch
        {
            0 =>
            [
                new(nameof(FireQuizLifestyle.Minimal), "Minimal / frugal", "Keep spending intentionally low."),
                new(nameof(FireQuizLifestyle.Moderate), "Moderate", "Cover comfortable basics with a balanced budget."),
                new(nameof(FireQuizLifestyle.Comfortable), "Comfortable", "Maintain flexibility with few major sacrifices."),
                new(nameof(FireQuizLifestyle.Luxury), "Higher spending", "Plan for more travel, experiences, or financial margin."),
                new(nameof(FireQuizLifestyle.NotSure), "Not sure yet", "Keep the recommendation broad for now.")
            ],
            1 =>
            [
                new(nameof(FireQuizWorkPreference.QuitCompletely), "Leave paid work", "Build toward fully funding your lifestyle from investments."),
                new(nameof(FireQuizWorkPreference.PartTime), "Work part-time", "Use some earned income for expenses, benefits, or purpose."),
                new(nameof(FireQuizWorkPreference.Coast), "Shift into coast mode", "Let investments grow while lower-stress work covers today."),
                new(nameof(FireQuizWorkPreference.Flexible), "Keep my options open", "Create room to change how and when you work."),
                new(nameof(FireQuizWorkPreference.NotSure), "Not sure yet", "Explore paths with different relationships to work.")
            ],
            2 =>
            [
                new(nameof(FireQuizTimeline.WithinFiveYears), "Within 5 years", "I have a near-term target."),
                new(nameof(FireQuizTimeline.FiveToTenYears), "In 5–10 years", "I want a focused medium-term plan."),
                new(nameof(FireQuizTimeline.TenToTwentyYears), "In 10–20 years", "I have time to balance growth and flexibility."),
                new(nameof(FireQuizTimeline.TwentyPlusYears), "More than 20 years", "I can give compound growth a long runway."),
                new(nameof(FireQuizTimeline.NotSure), "Not sure yet", "I am still exploring what is realistic.")
            ],
            3 =>
            [
                new(nameof(FireQuizPrimaryGoal.RetireEarly), "Reach FI as soon as practical", "Prioritize the timeline."),
                new(nameof(FireQuizPrimaryGoal.FinancialSecurity), "Build financial security", "Prioritize resilience and a balanced foundation."),
                new(nameof(FireQuizPrimaryGoal.MaintainLifestyle), "Maintain my lifestyle", "Prioritize spending capacity and margin."),
                new(nameof(FireQuizPrimaryGoal.Flexibility), "Create more flexibility", "Prioritize options and work-life balance."),
                new(nameof(FireQuizPrimaryGoal.NotSure), "Not sure yet", "Compare several reasonable starting points.")
            ],
            _ => []
        };

        var selectedValue = QuestionIndex switch
        {
            0 => lifestyle?.ToString(),
            1 => workPreference?.ToString(),
            2 => timeline?.ToString(),
            3 => primaryGoal?.ToString(),
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
        var defaults = calculatorDefaultsService.Current;
        var answers = new FireQuizAnswers(
            lifestyle ?? FireQuizLifestyle.NotSure,
            workPreference ?? FireQuizWorkPreference.NotSure,
            timeline ?? FireQuizTimeline.NotSure,
            primaryGoal ?? FireQuizPrimaryGoal.NotSure,
            defaults.CurrentAge,
            defaults.RetirementAge);
        var recommendation = FireQuizRecommender.Recommend(answers);

        PrimaryRecommendation = CreateOption(recommendation.Primary);
        AlternativeRecommendations.Clear();
        foreach (var alternative in recommendation.Alternatives)
        {
            AlternativeRecommendations.Add(CreateOption(alternative));
        }

        RecommendationConfidenceText = recommendation.Confidence switch
        {
            FireQuizConfidence.High => "Your answers point clearly to this starting path.",
            FireQuizConfidence.Medium => "This is a useful starting path, with nearby alternatives worth comparing.",
            _ => "Your answers are broad, so compare the alternatives before choosing."
        };
        onboardingService.SetRecommendation(recommendation.Primary.CalculatorId);
        IsRecommendationVisible = true;
    }

    private QuizRecommendationOption CreateOption(FireQuizMatch match)
    {
        return new QuizRecommendationOption(
            match.CalculatorId,
            match.Title,
            match.Reason,
            match.Description,
            string.Join(Environment.NewLine, match.Benefits.Select(benefit => $"• {benefit}")),
            catalog.GetRequired(match.CalculatorId).IconGlyph);
    }

    private DraftRecord CreateRecommendedDraft(string calculatorId)
    {
        object payload = calculatorId switch
        {
            "lean-fire" => calculatorDefaultsService.LeanFire,
            "fat-fire" => calculatorDefaultsService.FatFire,
            "barista-fire" => calculatorDefaultsService.BaristaFire,
            "coast-fire" => calculatorDefaultsService.CoastFire,
            "reverse-fire" => calculatorDefaultsService.ReverseFire,
            _ => calculatorDefaultsService.StandardFire
        };

        return new DraftRecord(
            calculatorId,
            1,
            JsonSerializer.Serialize(payload, payload.GetType()),
            DateTime.UtcNow);
    }
}
