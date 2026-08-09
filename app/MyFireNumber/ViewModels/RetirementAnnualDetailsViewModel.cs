using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFireNumber.ViewModels;

public sealed class RetirementAnnualDetailsViewModel : ObservableObject, IQueryAttributable
{
    public ObservableCollection<RetirementAnnualDetailItem> Years { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Years.Clear();
        if (!query.TryGetValue("details", out var value)
            || value is not IEnumerable<RetirementAnnualDetailItem> details)
        {
            return;
        }

        foreach (var detail in details)
        {
            Years.Add(detail);
        }
    }
}
