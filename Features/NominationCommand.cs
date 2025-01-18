
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using cs2_rockthevote.Core;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("nominate", "nominate a map to rtv")]
        public void OnNominate(CCSPlayerController? player, CommandInfo command)
        {
            string map = command.GetArg(1).Trim().ToLower();
            _nominationManager.CommandHandler(player!, map);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectNominate(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;

            if(player != null)
                _nominationManager.PlayerDisconnected(player);
            
            return HookResult.Continue;
        }
    }

    public class NominationCommand : IPluginDependency<Plugin, Config>
    {
        Dictionary<CCSPlayerController, string> Nominations = new();
        private RtvConfig _config = new();
        private GameRules _gamerules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private MapLister _mapLister;
        private List<string> _nominatedMaps = new();

        public NominationCommand(MapLister mapLister, GameRules gamerules, StringLocalizer localizer, PluginState pluginState, MapCooldown mapCooldown)
        {
            _mapLister = mapLister;
            _gamerules = gamerules;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _mapCooldown.EventCooldownRefreshed += OnMapsLoaded;
        }


        public void OnMapStart(string map)
        {
            Nominations.Clear();
            _nominatedMaps.Clear();
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            /*
            nominationMenu = new("Nomination");
            foreach (var map in _mapLister.Maps!)
            {
                if(map.Name == Server.MapName)
                {
                    nominationMenu.AddMenuOption($"{map.Name} (Current Map)", (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        Nominate(player, option.Text);
                    }, true);
                    continue;
                }

                // if map is in cooldown, we add it to the menu with a cooldown message
                if(_mapCooldown.IsMapInCooldown(map.Name) && _mapCooldown.GetMapCooldown(map.Name) != -1)
                    nominationMenu.AddMenuOption($"{map.Name} (Recent Played {_mapCooldown.GetMapCooldown(map.Name)})", (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        Nominate(player, option.Text);
                    }, true);

                // if map is not in cooldown, we add it to the menu
                else
                    nominationMenu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        Nominate(player, option.Text);
                    }, false);
            }
            nominationMenu.ExitButton = true;
            */
        }

        public void CommandHandler(CCSPlayerController? player, string map)
        {
            if (player is null)
                return;

            map = map.ToLower().Trim();
            if (_pluginState.DisableCommands || !_config.NominationEnabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gamerules.WarmupRunning)
            {
                if (!_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }
            else if (_config.MinRounds > 0 && _config.MinRounds > _gamerules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config!.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }

            if(_nominatedMaps.Count >= _config.MaxNominateMap)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.max-nominate-reach", _config.MaxNominateMap));
                return;
            }

            if (string.IsNullOrEmpty(map))
            {
                OpenNominationMenu(player!, "");
            }
            else
            {
                Nominate(player, map);
            }
        }

        public void OpenNominationMenu(CCSPlayerController player, string mapname = "")
        {
            // MenuManager.OpenChatMenu(player!, nominationMenu!);
            var menu = new ChatMenu("Nomination");

            if(mapname == "")
            {
                foreach (var map in _mapLister.Maps!)
                {
                    if(map.Name == Server.MapName)
                    {
                        menu.AddMenuOption($"{map.Name} (Current Map)", (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, true);
                        continue;
                    }

                    // if map is in cooldown, we add it to the menu with a cooldown message
                    if(_mapCooldown.IsMapInCooldown(map.Name) && _mapCooldown.GetMapCooldown(map.Name) != -1)
                        menu.AddMenuOption($"{map.Name} (Recent Played {_mapCooldown.GetMapCooldown(map.Name)})", (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, true);

                    // if map is not in cooldown, we add it to the menu
                    else
                        menu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, false);
                }
            }

                // we got them bois
            else 
            {
                foreach (var map in _mapLister.Maps!.Where(x => x.Name.Contains(mapname, StringComparison.OrdinalIgnoreCase)))
                {
                    if(map.Name == Server.MapName)
                    {
                        menu.AddMenuOption($"{map.Name} (Current Map)", (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, true);
                        continue;
                    }

                    // if map is in cooldown, we add it to the menu with a cooldown message
                    if(_mapCooldown.IsMapInCooldown(map.Name) && _mapCooldown.GetMapCooldown(map.Name) != -1)
                        menu.AddMenuOption($"{map.Name} (Recent Played {_mapCooldown.GetMapCooldown(map.Name)})", (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, true);

                    // if map is not in cooldown, we add it to the menu
                    else
                        menu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            Nominate(player, option.Text);
                        }, false);
                }
            }
            menu.ExitButton = true;
            menu.Open(player);
        }

        void Nominate(CCSPlayerController player, string map)
        {
            if (map == Server.MapName)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            if (_mapCooldown.IsMapInCooldown(map))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }

            if (_mapLister.Maps!.Select(x => x.Name).FirstOrDefault(x => x.ToLower() == map) is null)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                OpenNominationMenu(player!, map);
                return;

            }

            if(_nominatedMaps.Contains(map))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.already-nominated", map));
                return;
            }

            if(player == null)
                return;

            if (!Nominations.ContainsKey(player))
                Nominations.Add(player, map);

            // if player is in nomination list, then we have to remove previous nomination from _nominatedMaps list.
            if (Nominations.ContainsKey(player) && _nominatedMaps.Contains(Nominations[player]))
                _nominatedMaps.Remove(Nominations[player]);

            Nominations[player] = map;
            _nominatedMaps.Add(map);

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName, map));
        }

        public List<string> GetNominationList()
        {
            return _nominatedMaps;
        }

        public void PlayerDisconnected(CCSPlayerController player)
        {
            if (Nominations.ContainsKey(player))
            {
                if(_nominatedMaps.Contains(Nominations[player]))
                    _nominatedMaps.Remove(Nominations[player]);

                Nominations.Remove(player);
            }
        }

        public void NominationOnVoteEnd()
        {
            Nominations.Clear();
            _nominatedMaps.Clear();
        }
    }
}
