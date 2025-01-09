using System;
using System.IO;
using Modding;

namespace DeathCounter
{
    public class DeathCounterMod : Mod
    {
        private static DeathCounterMod? _instance;

        internal static DeathCounterMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(DeathCounterMod)} was never constructed");
                }
                return _instance;
            }
        }

        public override string GetVersion() => "1";

        // Where to save the .txt file (Will save in the game installation directory)
        private string _deathCountFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deaths.txt");

        // storing total deaths for this session
        private int _deathCount = 0;


        public DeathCounterMod() : base("DeathCounter")
        {
            _instance = this;
        }

        // Load the mod here
        public override void Initialize()
        {
            _deathCount = ReadDeathCountFromFile();
            ModHooks.AfterPlayerDeadHook += CountDeath;
        }

        //Unload the mod
        public void Unload()
        {
            ModHooks.AfterPlayerDeadHook -= CountDeath;
        }

        // Increment the death and store it in a txt file, this allows it to be used for streams in OBS
        // Additionally it allows it to be editable if moving to this mod from manually tracking or something
        private void CountDeath()
        {
            // Increment the count
            _deathCount++;

            // Try to save it to the txt file
            try
            {
                File.WriteAllText(_deathCountFilePath, _deathCount.ToString()); // Write the death count to the file
            }
            catch (Exception ex)
            {
                // Log the exception (you could use your mod's logging system if available)
                Log($"Error writing death count to file: {ex.Message}");
            }
        }

        // Try to read the existing death count if it exits, if not its 0
        private int ReadDeathCountFromFile()
        {
            try
            {
                if (File.Exists(_deathCountFilePath))
                {
                    var content = File.ReadAllText(_deathCountFilePath);
                    if (int.TryParse(content, out int savedDeathCount))
                    {
                        return savedDeathCount; // Return the stored death count
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error reading death count from file: {ex.Message}");
            }

            return 0; // Default to 0 if reading fails
        }
    }
}
