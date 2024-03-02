namespace CourseLibrary.API.Helpers;

public static class DateTimeOffsetExtensions
{
    public static int GetCurrentAge(this DateTimeOffset dateOfBirth, DateTimeOffset? dateOfDeath)
    {
        DateTime dateForComparison = DateTime.UtcNow;
        if (dateOfDeath != null)
        {
            dateForComparison = dateOfDeath.Value.DateTime;
        }
        int age = dateForComparison.Year - dateOfBirth.Year;

        if (dateForComparison < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
