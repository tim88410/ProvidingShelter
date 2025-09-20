namespace ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate
{
    public enum CrossCategoryType : byte
    {
        AgeGroup = 1,
        Industry = 2
    }

    public enum NationalityCode : byte
    {
        Indonesia = 1,
        Thailand = 2,
        Vietnam = 3,
        Philippines = 4,
        Malaysia = 5,
        Other = 6,
        OtherCountry = 9
    }
}
