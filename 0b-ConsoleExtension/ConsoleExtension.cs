using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleExtension
{
    public class ConsoleExtension : ConsoleCmdAbstract
    {
        public override bool IsExecuteOnClient => true;

        public override bool AllowedInMainMenu => true;

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count < 1)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No sub command given.");
                return;
            }

            PrintParams(_params);

            string text = _params[0].ToLowerInvariant();
            string text2 = text;
            switch (text2)
            {
                case "cvarlog":
                    {
                        CVarLog();
                        break;
                    }
                case "clearactivequests":
                    {
                        ClearActiveQuests();
                        break;
                    }
                case "setcvar":
                    {
                        SetCVar(_params);
                        break;
                    }
                case "navlist":
                    {
                        navList();
                        break;
                    }

                case "commands":
                    {
                        Log.Out("Commands: cvarlog,clearactivequests,setvar,navlist");
                        break;
                    }

                default:
                    {
                        SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Invalid sub command \"" + _params[0] + "\".");
                        break;
                    }
            }
        }

        protected override string[] getCommands()
        {
            return new string[1] { "rise" };
        }

        protected override string getDescription()
        {
            throw new NotImplementedException();
        }

        private void CVarLog()
        {
            EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
            foreach (var cvar in player.Buffs.CVars)
            {
                Log.Out(string.Format("CVar: {0} = {1}", cvar.Key, cvar.Value.ToString()));
            }
        }

        private void ClearActiveQuests()
        {
            EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
            List<Quest> quests = new List<Quest>();
            foreach (Quest q in player.QuestJournal.quests)
            {
                if (q != null)
                {
                    if (q.Active == true)
                    {
                        quests.Add(q);
                    }
                }
            }

            foreach (Quest q in quests)
            {
                player.QuestJournal.ForceRemoveQuest(q);
            }
        }

        private void SetCVar(List<string> _params)
        {
            _params.RemoveAt(0);            
            EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
            string cvar = _params[0];
            float value = float.Parse(_params[1]);

            Log.Out("Setting: " + cvar + " = " + value); 
            player.SetCVar(cvar, value);
           
        }

        private void navList()
        {            
            EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
            NavObjectManager navObjectManager = NavObjectManager.Instance;
            foreach (var navObject in navObjectManager.NavObjectList)
            {
                Log.Out("Nav object : " + navObject.DisplayName + " at loc = " + navObject.GetPosition());
            }
        }

        private void PrintParams(List<string> _params)
        {
            Log.Out("Printing Params");
            foreach (var param in _params)
            {
                Log.Out("Parameter: " + param);
            }
        }
    }
}

