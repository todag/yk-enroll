using Yubico.YubiKey;

namespace YKEnroll.Lib
{
    /// <summary>
    ///     Simple interface to allow custom prompts to be implemented.
    /// </summary>
    public interface IKeyCollectorPrompt
    {
        public KeyCollectorResult Prompt(KeyEntryData keyEntryData);
    }
}
