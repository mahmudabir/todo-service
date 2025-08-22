namespace Shared.Models.Results;

public class Error
{
    public string Key { get; set; }
    public string Message { get; set; }
    public IDictionary<string, string[]> Errors { get; set; }

    public static Error Create(string key, params string[] messages)
    {
        return new Error
        {
            Key = key,
            Message = messages.FirstOrDefault() ?? string.Empty,
            Errors = new Dictionary<string, string[]>
            {
                {
                    key, messages
                }
            }
        };
    }

    public Error AddError(string key, params string[] messages)
    {
        Errors.Add(key, messages);
        return this;
    }
}