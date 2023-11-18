using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace innBot.Services;

public static class InnInfoProcessor
{
    private static readonly HttpClient client = new HttpClient();
    public static async Task<string> GetInfoByInn(string inn)
    {
        var url = $"https://vbankcenter.ru/contragent/api/web/counterparty/filter?page=0&size=20&searchStr={inn}&withCounter=true";
        string jsonResponse = await GetJsonResponse(url);
        string extractedInfo = ParseJsonResponse(jsonResponse, inn);
        return extractedInfo;
    }

    private static  async Task<string> GetJsonResponse(string url)
    {
        using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    private static string ParseJsonResponse(string jsonResponse, string inn)
    {
        JObject responseObject = JObject.Parse(jsonResponse);
        string extractedInfo;
        
        if (responseObject["content"] is JArray contentArray && contentArray.Count > 0)
        {
            JObject firstObject = (JObject)contentArray[0];
            if (firstObject["inn"] is null || !IsInnMatching(inn, (string)firstObject["inn"]))
            {
                return "Такой компании не существует\n";
            }
            
            if (firstObject["name"] != null)
            {
                string nameValue = (string)firstObject["name"];
                
                extractedInfo = $"Название компании: {nameValue}\n";
                
                if (firstObject["address"] != null)
                {
                    string addressValue = (string)firstObject["address"];
                    extractedInfo += $"Адрес: {addressValue}\n";
                }
                else
                {
                    extractedInfo += "Адрес неизвестен";
                }
            }
            else
            {
                extractedInfo = "Такой компании не существует\n";
            }
        }
        else
        {
            return "Такой компании не существует\n";
        }

        return extractedInfo;
    }

    private static bool IsInnMatching(string inn, string innFromResponse)
    {
        string pattern = @"<em>(\d+)</em>";
        Match match = Regex.Match(innFromResponse, pattern);
        
        if (match.Success)
        {
            return match.Groups[1].Value.Equals(inn);
        }

        return false;
    }
}