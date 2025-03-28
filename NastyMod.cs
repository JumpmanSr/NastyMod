/**
 *      _   _           _         __  __           _ 
 *     | \ | | __ _ ___| |_ _   _|  \/  | ___   __| |
 *     |  \| |/ _` / __| __| | | | |\/| |/ _ \ / _` |
 *     | |\  | (_| \__ \ |_| |_| | |  | | (_) | (_| |
 *     |_| \_|\__,_|___/\__|\__, |_|  |_|\___/ \__,_|
 *                          |___/                    
 *     
 *     a MelonLoader mod for the game Schedule I
 */
using System;
using System.Collections.Generic;

using UnityEngine;
using MelonLoader;
using HarmonyLib;

using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Employees;

using JetBrains.Annotations;
using Il2CppScheduleOne.NPCs;

public class NastyModClass : MelonMod
{
    public static NastyModClass Instance { get; private set; }

    private bool _isOpen = false;

    private int buttonWidth = 90;
    private int buttonHeight = 30;
    private int buttonSpacing = 10;

    private int menuWidth = 650;
    private int menuHeight = 500;
    private int menuSpacing = 15;

    private int tabWidth = 0;
    private int tabHeight = 0;

    private int menuTab = 0;
    private readonly List<string> menuTabs = new List<string> { "Player", "World", "Spawner", "Misc", "Employees", "Credits" };

    private Dictionary<string, List<string>> itemTree;
    private List<string> propertys;

    private int _moddedStackSize = 100;
    private int _moddedCash = 1000;
    private int _moddedBalance = 1000;

    private HarmonyLib.Harmony harmony;

    private bool _godMode = false;
    private bool _infiniteEnergy = false;
    private bool _infiniteStamina = false;
    private bool _neverWanted = false;
    private float _moveSpeedMultiplier = 1f;
    private float _crouchSpeedMultiplier = 0.6f;
    private float _jumpMultiplier = 1f;

    private string _selectedCategory = "Product";
    private Vector2 _itemSpawnerCategoryScrollPosition;
    private int _itemAmount = 1;

    private string _selectedProperty = "barn";
    private Vector2 _employeeSpawnerPropertyScrollPosition;

    private GUIStyle _titleStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _buttonStyle;

    public void SendLoggerMsg(string msg)
    {
        MelonLogger.Msg(msg);
    }

    [Obsolete]
    public override void OnApplicationStart()
    {
        harmony = new HarmonyLib.Harmony("com.nastymod");

        MelonLogger.Msg("Initializing...");

        UnlockDebugMode();
    }
    private static void UnlockDebugMode()
    {
        try
        {
            MelonLogger.Msg("Attempting to unlock Debug Mode...");
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(DebugPatch), (string)null);
            MelonLogger.Msg("Debug Mode unlocked!");
        }
        catch (Exception value)
        {
            MelonLogger.Error($"Error unlocking Debug Mode: {value}");
        }
    }

    public override void OnInitializeMelon()
    {
        Instance = this;

        loadAllItems();
        loadAllPropertys();
    }

    private void loadAllItems()
    {
        NastyMod.JsonLoader itemLoader = new NastyMod.JsonLoader();
        itemTree = itemLoader.LoadItems();

        MelonLogger.Msg($"Loaded {itemTree.Count} item categories");
    }

    private void loadAllPropertys()
    {
        NastyMod.JsonLoader propertyLoader = new NastyMod.JsonLoader();
        propertys = propertyLoader.LoadPropertys();

        MelonLogger.Msg($"Loaded {propertys.Count} propertys");
    }

    public override void OnUpdate()
    {
        if (_godMode)
        {
            Player.Local.Health.RecoverHealth(100);
        }

        if (_infiniteEnergy)
        {
            Player.Local.Energy.RestoreEnergy();
        }

        if (_infiniteStamina)
        {
            PlayerMovement.Instance.ChangeStamina(100);
        }

        if (_neverWanted)
        {
            Player.Local.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.None);
        }

        // SetStackSize();

        if (Input.GetKeyDown(KeyCode.F11))
            _isOpen = !_isOpen;
    }

    public override void OnGUI()
    {
        if (!_isOpen) return;

        // **Styles**
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fixedWidth = 80,
            fixedHeight = 24
        };
        // *********

        GUILayout.BeginArea(new Rect(50, 50, menuWidth, menuHeight), GUI.skin.box);

        // **Title**
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 2), 40));
        GUILayout.Label("NastyMod v1", _titleStyle);
        GUILayout.EndArea();
        // *********

        // **Dynamic Tab Buttons**
        GUILayout.BeginArea(new Rect(15, 55, menuWidth - (menuSpacing * 2), 40));
        GUILayout.BeginHorizontal();
        for (int i = 0; i < menuTabs.Count; i++)
        {
            if (menuTabs[i] == "Player" && !PlayerMovement.InstanceExists)
                continue;

            if (GUILayout.Button(menuTabs[i], GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                menuTab = i;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        // ***********************

        // **Tabs**
        GUILayout.BeginArea(new Rect(15, 95, menuWidth - (menuSpacing * 2), menuHeight - 110), GUI.skin.box);
        switch (menuTab)
        {
            case 0:
                if (PlayerMovement.InstanceExists) { 
                    RenderPlayerTab();
                    break;
                } else
                {
                    menuTab = 1;
                    RenderWorldTab();
                    break;
                }
            case 1:
                RenderWorldTab();
                break;
            case 2:
                RenderSpawnerTab();
                break;
            case 3:
                RenderMiscTab();
                break;
            case 4:
                RenderEmployeesTab();
                break;
            case 5:
                RenderCreditsTab();
                break;
        }
        GUILayout.EndArea();
        // ********

        GUILayout.EndArea();
    }

    private void RenderPlayerTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // Tab Title
        GUILayout.Label("Player", _headerStyle);

        // Spacer
        GUILayout.Space(10);

        // ** God Mode toggle
        GUILayout.BeginHorizontal();
        GUILayout.Label("God Mode");
        string status_godMode = _godMode ? "On" : "Off";
        if (GUILayout.Button(status_godMode, _buttonStyle))
        {
            _godMode = !_godMode;
            MelonLogger.Msg("God Mode toggled!");
        }
        GUILayout.EndHorizontal();

        // ** Infinite Energy toggle
        GUILayout.BeginHorizontal();
        GUILayout.Label("Infinite Energy");
        string status_infiniteEnergy = _infiniteEnergy ? "On" : "Off";
        if (GUILayout.Button(status_infiniteEnergy, _buttonStyle))
        {
            _infiniteEnergy = !_infiniteEnergy;
            MelonLogger.Msg("Infinite Energy toggled!");
        }
        GUILayout.EndHorizontal();

        // ** Infinite Stamina toggle
        GUILayout.BeginHorizontal();
        GUILayout.Label("Infinite Stamina");
        string status_infiniteStamina = _infiniteStamina ? "On" : "Off";
        if (GUILayout.Button(status_infiniteStamina, _buttonStyle))
        {
            _infiniteStamina = !_infiniteStamina;
            MelonLogger.Msg("Infinite Stamina toggled!");
        }
        GUILayout.EndHorizontal();

        // ** Never wanted toggle
        GUILayout.BeginHorizontal();
        GUILayout.Label("Never Wanted");
        string status_neverWanted = _neverWanted ? "On" : "Off";
        if (GUILayout.Button(status_neverWanted, _buttonStyle))
        {
            _neverWanted = !_neverWanted;
            MelonLogger.Msg("Never Wanted toggled!");
        }
        GUILayout.EndHorizontal();

        // Spacer
        GUILayout.Space(20);

        // ** Move speed slider
        GUILayout.BeginHorizontal();
        GUILayout.Label("Move speed multiplier: " + _moveSpeedMultiplier);
        _moveSpeedMultiplier = GUILayout.HorizontalSlider(_moveSpeedMultiplier, 1f, 10f, GUILayout.Width(180), GUILayout.Height(10));
        PlayerMovement.Instance.MoveSpeedMultiplier = _moveSpeedMultiplier;
        GUILayout.EndHorizontal();

        // ** Crouch speed slider
        GUILayout.BeginHorizontal();
        GUILayout.Label("Crouch speed multiplier: " + _crouchSpeedMultiplier);
        _crouchSpeedMultiplier = (float)GUILayout.HorizontalSlider(_crouchSpeedMultiplier, 0.6f, 10f, GUILayout.Width(180), GUILayout.Height(10));
        PlayerMovement.Instance.crouchSpeedMultipler = _crouchSpeedMultiplier;
        GUILayout.EndHorizontal();

        // ** Jump Multiplier slider
        GUILayout.BeginHorizontal();
        GUILayout.Label("Jump Multiplier: " + _jumpMultiplier);
        _jumpMultiplier = GUILayout.HorizontalSlider(_jumpMultiplier, 1f, 10f, GUILayout.Width(180), GUILayout.Height(10));
        PlayerMovement.JumpMultiplier = _jumpMultiplier;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void RenderWorldTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // Tab Title
        GUILayout.Label("World", _headerStyle);

        // Spacer
        GUILayout.Space(10);

        // ** Coming soon...
        GUILayout.Label("Coming soon...");

        GUILayout.EndArea();
    }

    private void RenderSpawnerTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // Tab Title
        GUILayout.Label("Spawner", _headerStyle);

        #region Left Side
        GUILayout.BeginArea(new Rect(0, 40, (menuWidth - (menuSpacing * 4) - menuSpacing) / 4, menuHeight - 110 - (menuSpacing * 2) - 40));
        
        // ** Item Category dropdown
        _itemSpawnerCategoryScrollPosition = GUILayout.BeginScrollView(_itemSpawnerCategoryScrollPosition, GUILayout.Width((menuWidth - (menuSpacing * 4) - menuSpacing) / 4), GUILayout.Height(menuHeight - 110 - (menuSpacing * 2) - 80 - menuSpacing));
        foreach (var category in itemTree)
        {
            if (GUILayout.Button(category.Key))
            {
                _selectedCategory = category.Key;
            }
        }
        GUILayout.EndScrollView();

        // Spacer
        GUILayout.Space(10);

        // ** Item Amount slider
        GUILayout.Label($"Item Amount: {_itemAmount}");
        _itemAmount = (int)GUILayout.HorizontalSlider(_itemAmount, 1, _moddedStackSize);

        GUILayout.EndArea();
        #endregion

        #region Right side
        GUILayout.BeginArea(new Rect(((menuWidth - (menuSpacing * 4) - menuSpacing) / 4) + menuSpacing, 40, ((menuWidth - (menuSpacing * 4) - menuSpacing) / 4) * 3, menuHeight - 110 - (menuSpacing * 2) - 40));
        // ** Item buttons
        int columns = 3;
        int currentColumn = 0;
        GUILayout.BeginHorizontal();
        foreach (var category in itemTree)
        {
            if (category.Key == _selectedCategory)
            {
                foreach (var item in category.Value)
                {
                    if (currentColumn == columns)
                    {
                        currentColumn = 0;

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }

                    int availableWidth = ((menuWidth - (menuSpacing * 4) - menuSpacing) / 4) * 3;
                    if (GUILayout.Button(item, GUILayout.Width((availableWidth - (5 * columns-1)) / columns)))
                    {
                        Il2CppScheduleOne.Console.AddItemToInventoryCommand command = new Il2CppScheduleOne.Console.AddItemToInventoryCommand();
                        Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();
                        args.Add(item);
                        args.Add(_itemAmount.ToString());
                        command.Execute(args);
                    }

                    currentColumn++;
                }
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        #endregion

        GUILayout.EndArea();
    }

    private void RenderMiscTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // Tab Title
        GUILayout.Label("Misc", _headerStyle);

        // Spacer
        GUILayout.Space(10);

        // ** Default Stack Limit slider
        GUILayout.Label("Default Stack Limit: " + _moddedStackSize);
        _moddedStackSize = (int)GUILayout.HorizontalSlider(_moddedStackSize, 10, 250);

        // Spacer
        GUILayout.Space(10);

        // Note
        GUILayout.Label("Change equipped quality type");
        GUILayout.Label("NOTE: You need to have the item equipped for this feature to work.");

        // Spacer
        GUILayout.Space(10);

        // ** Quality type buttons
        GUILayout.BeginHorizontal();
        foreach (var qualityType in Enum.GetValues(typeof(EQuality)))
        {
            if (GUILayout.Button(qualityType.ToString(), _buttonStyle))
            {
                Il2CppScheduleOne.Console.SetQuality command = new Il2CppScheduleOne.Console.SetQuality();
                Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();
                args.Add(qualityType.ToString());
                command.Execute(args);
            }
        }
        GUILayout.EndHorizontal();

        // Spacer
        GUILayout.Space(20);

        // ** Cash Modifier
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Cash: " + _moddedCash);
        _moddedCash = (int) GUILayout.HorizontalSlider(_moddedCash, 0, 100000, GUILayout.Width((menuWidth - (30 * 2)) - 170), GUILayout.Height(20));
        GUILayout.EndVertical();
        if (GUILayout.Button("Add", _buttonStyle))
        {
            Il2CppScheduleOne.Console.ChangeCashCommand command = new Il2CppScheduleOne.Console.ChangeCashCommand();
            Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();

            args.Add(_moddedCash.ToString());

            command.Execute(args);
        }
        if (GUILayout.Button("Remove", _buttonStyle))
        {
            Il2CppScheduleOne.Console.ChangeCashCommand command = new Il2CppScheduleOne.Console.ChangeCashCommand();
            Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();

            args.Add($"-{_moddedCash.ToString()}");

            command.Execute(args);
        }
        GUILayout.EndHorizontal();

        // Spacer
        GUILayout.Space(10);

        // ** Balance Modifier
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Balance: " + _moddedBalance);
        _moddedBalance = (int)GUILayout.HorizontalSlider(_moddedBalance, 0, 100000, GUILayout.Width((menuWidth - (30 * 2)) - 170), GUILayout.Height(20));
        GUILayout.EndVertical();
        if (GUILayout.Button("Add", _buttonStyle))
        {
            Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand command = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
            Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();

            args.Add(_moddedBalance.ToString());

            command.Execute(args);
        }
        if (GUILayout.Button("Remove", _buttonStyle))
        {
            Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand command = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
            Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();

            args.Add($"-{_moddedBalance.ToString()}");

            command.Execute(args);
        }
        GUILayout.EndHorizontal();

        // ** Max Dealer Customers slider
        /* GUILayout.BeginHorizontal();
        GUILayout.Label("Max Dealer Customers: " + _moddedDealerMaxCustomers);
        _moddedDealerMaxCustomers = (int)GUILayout.HorizontalSlider(_moddedDealerMaxCustomers, 1, 100);
        GUILayout.EndHorizontal(); */

        // ** Deaddrop Wait Per Item slider
        /* GUILayout.BeginHorizontal();
        GUILayout.Label("Deaddrop Wait Per Item: " + __moddedDeaddropWaitPerItem);
        __moddedDeaddropWaitPerItem = (int)GUILayout.HorizontalSlider(__moddedDeaddropWaitPerItem, 1, 100);
        GUILayout.EndHorizontal(); */

        // Spacer
        GUILayout.Space(20);

        // ** Unlock all achievements button
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unlock all achievements", GUILayout.Width(menuWidth - (30 * 2)), GUILayout.Height(24)))
        {
            foreach (var achievement in Enum.GetValues(typeof(AchievementManager.EAchievement)))
            {
                AchievementManager.Instance.UnlockAchievement((AchievementManager.EAchievement)achievement);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void RenderEmployeesTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // Tab Title
        GUILayout.Label($"Employees on Property \"{_selectedProperty}\"", _headerStyle);

        // Spacer
        GUILayout.Space(10);

        #region Left Side
        GUILayout.BeginArea(new Rect(0, 40, (menuWidth - (menuSpacing * 4) - menuSpacing) / 4, menuHeight - 110 - (menuSpacing * 2) - 40));
        // ** Property buttons
        _employeeSpawnerPropertyScrollPosition = GUILayout.BeginScrollView(_employeeSpawnerPropertyScrollPosition, GUILayout.Width((menuWidth - (menuSpacing * 4) - menuSpacing) / 4), GUILayout.Height(menuHeight - 110 - (menuSpacing * 2) - 80 - menuSpacing));
        foreach (var property in propertys)
        {
            if (GUILayout.Button(property))
            {
                _selectedProperty = property;
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        #endregion

        #region Right side
        GUILayout.BeginArea(new Rect(((menuWidth - (menuSpacing * 4) - menuSpacing) / 4) + menuSpacing, 40, ((menuWidth - (menuSpacing * 4) - menuSpacing) / 4) * 3, menuHeight - 110 - (menuSpacing * 2) - 40));
        
        // ** Employee buttons
        Dictionary<string, Dictionary<string, List<Employee>>> employees = new Dictionary<string, Dictionary<string, List<Employee>>>();
        employees = GetAlLEmployees();

        if (!employees.ContainsKey(_selectedProperty))
        {
            GUILayout.Label($"No Employees on Property \"{_selectedProperty}\"");
        } else
        {
            foreach (var employeeType in employees[_selectedProperty])
            {
                GUILayout.Label(employeeType.Key);
                foreach (var employee in employeeType.Value)
                {
                    string currentEmployeeType = employee.EmployeeType.ToString();
                    string currentEmployeeName = employee.FirstName;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{currentEmployeeType} {currentEmployeeName}");

                    if (GUILayout.Button("TP", GUILayout.Width(60)))
                    {
                        employee.gameObject.transform.position = Player.Local.transform.position;
                    }
                    if (GUILayout.Button("Fire", GUILayout.Width(60)))
                    {
                        employee.SendFire();
                        employee.Movement.MoveSpeedMultiplier = 2.5f;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
            }
        }
        
        // Spacer
        GUILayout.Space(10);

        // ** Add Employee buttons
        GUILayout.BeginHorizontal();
        foreach (var employeeType in Enum.GetValues(typeof(EEmployeeType)))
        {
            if (GUILayout.Button($"Add {employeeType.ToString()}"))
            {
                Il2CppScheduleOne.Console.AddEmployeeCommand command = new Il2CppScheduleOne.Console.AddEmployeeCommand();
                Il2CppSystem.Collections.Generic.List<string> args = new Il2CppSystem.Collections.Generic.List<string>();

                args.Add(employeeType.ToString());
                args.Add(_selectedProperty);

                command.Execute(args);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
        #endregion

        GUILayout.EndArea();
    }

    private void RenderCreditsTab()
    {
        GUILayout.BeginArea(new Rect(15, 15, menuWidth - (menuSpacing * 4), menuHeight - 110 - (menuSpacing * 2)));

        // **Tab Title**
        GUILayout.Label("Credits", _headerStyle);

        // **Spacer**
        GUILayout.Space(10);

        // **Creator**
        GUILayout.Label("This mod was created by nasty.codes");
        GUILayout.Label("Discord: nasty.codes");

        // **Spacer**
        GUILayout.Space(20);

        // **Thanks**
        GUILayout.Label("Thanks to");
        GUILayout.Label("GitHub Copilot");
        GUILayout.Label("MelonLoader");

        GUILayout.EndArea();
    }

    public Dictionary<string, Dictionary<string, List<Employee>>> GetAlLEmployees()
    {
        Dictionary<string, Dictionary<string, List<Employee>>> employees = new Dictionary<string, Dictionary<string, List<Employee>>>();

        foreach (var employee in EmployeeManager.Instance.AllEmployees.ToArray())
        {
            // MelonLogger.Msg("Employee: " + employee.FirstName + " " + employee.LastName + " " + employee.EmployeeType + " " + employee.AssignedProperty.propertyCode);

            if (employees.ContainsKey(employee.AssignedProperty.propertyCode))
            {
                if (employees[employee.AssignedProperty.propertyCode].ContainsKey(employee.EmployeeType.ToString()))
                {
                    employees[employee.AssignedProperty.propertyCode][employee.EmployeeType.ToString()].Add(employee);
                }
                else
                {
                    employees[employee.AssignedProperty.propertyCode].Add(employee.EmployeeType.ToString(), new List<Employee> { employee });
                }
            }
            else
            {
                employees.Add(employee.AssignedProperty.propertyCode, new Dictionary<string, List<Employee>> { { employee.EmployeeType.ToString(), new List<Employee> { employee } } });
            }
        }
        return employees;
    }

    [HarmonyPatch]
    public static class DebugPatch
    {
        [HarmonyPatch(typeof(Debug), "get_isDebugBuild")]
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    [HarmonyPatch]
    public class StackLimitPatch
    {
        [HarmonyPatch(typeof(ItemInstance), "StackLimit", MethodType.Getter)]
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (NastyModClass.Instance != null)
            {
                __result = (int)NastyModClass.Instance._moddedStackSize;
            }
        }
    }
}
