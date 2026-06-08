namespace Collectary.Core.UseCases;

public class UsernameUniquifier
{
    public async Task<string> MakeUniqueAsync(string baseName, Func<string, Task<bool>> isTaken)
    {
        if (!await isTaken(baseName)) return baseName;

        var suffix = 1;
        string candidate;
        do
        {
            suffix++;
            candidate = $"{baseName}-{suffix}";
        }
        while (await isTaken(candidate));

        return candidate;
    }
}
