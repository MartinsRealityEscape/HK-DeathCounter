using System;
using System.IO;
using System.Linq;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using SFCore.Utils;

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

        public override string GetVersion() => "1.0.0.0";

        // Where to save the .txt file (Will save in the game installation directory)
        private string _deathCountFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deaths.txt");

        // storing total deaths for this session
        private int _deathCount = 0;
        
        // Check if the player has respawned after a dream death
        public bool HasRespawned = true;


        public DeathCounterMod() : base("DeathCounter")
        {
            _instance = this;
        }

        // Load the mod here
        public override void Initialize()
        {
            _deathCount = ReadDeathCountFromFile();
            ModHooks.AfterPlayerDeadHook += CountDeath;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
        }

        //Unload the mod
        public void Unload()
        {
            ModHooks.AfterPlayerDeadHook -= CountDeath;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChange;
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

        public void OnSceneChange(Scene from, Scene to)
        {
            var sceneName = to.name;

            if (sceneName == "Menu_Title")
            {
                // Return here as there will be no hero gameObject
                return;
            }

            // This FSM event detects TK dream death.
            GameObject hero = HeroController.instance.gameObject;

            if (hero == null)
            {
                return;
            }

            // Thanks to this reference https://github.com/royitaqi/HollowKnight.GodhomeWinLossTracker/blob/main/GodhomeWinLossTracker/MessageBus/Handlers/TKDeathAndStatusObserver.cs
            // Hook TK death event
            hero.transform.Find("Hero Death")
                .gameObject
                .LocateMyFSM("Hero Death Anim")
                .GetState("Anim Start")
                .AddMethod(Fsm_OnHeroDeathAnimStart);

            // Reset HasRespawned when entering a new scene
            HasRespawned = true;
        }

        public void Fsm_OnHeroDeathAnimStart()
        {
            // Check if the player hasn't respawned yet (to prevent multiple calls)
            if (!HasRespawned)
            {
                Log("Death already counted, ignoring subsequent calls.");
                return; // Exit early if the death was already counted
            }

            // Set HasRespawned to false (indicating death is ongoing and yet to respawn)
            HasRespawned = false;

            // Then count the death
            CountDeath();
        }
    }
}
