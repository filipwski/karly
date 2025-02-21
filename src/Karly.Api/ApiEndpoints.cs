namespace Karly.Api;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Cars
    {
        private const string Base = $"{ApiBase}/cars";

        public const string Get = $"{Base}/{{id:guid}}";
        public const string GetAll = Base;
        public const string Create = Base;
        public const string Generate = $"{Base}/Generate";
    }
}