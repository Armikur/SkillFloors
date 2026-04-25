# SkillFloors
SkillFloors is a Valheim Mod which creates a minimum skill level for each skill (the skill's "floor"). When you lose skill levels due to death, skill levels won't drop below their skill floors.

Skill floors start at level 0 and increase up at 15% the normal skill's rate. The rate is configurable.

## Installation
# r2Modman
I recommend using r2modman to install.

# Manual
If you're familiar with manually installing mods, go for it! Download the zip, and just put the SkillFloors.dll in BepInEx/plugins/SkillFloors.

## Features
* Slowly levels up a skill "floor" level for each skill.
* prevents skill levels from dropping below the floor on death
* skill floors are shown in the skill panel as a light blue number next to each skill name and small progress bar (light blue) under the skill's bar (red)
* configurable progress rate (available in BepInEx Configuration Manager)
* ServerSync for the progress rate.

## To(maybe)Do
* console commands
* Sound effect for skill floor level up

## Known issues
* The skill floor progress bar might disappear sometimes. This does not affect the actual skill floor's level progress, only the bar's display on the skills list.

## Other / Support
* You can find the github at: https://github.com/Armikur/SkillFloors
* If you have any issues, please open an issue on the github page or @Armikur on the [Valheim Modding Discord](https://discord.com/invite/GUEBuCuAMz). (I'm not a member of that team, but I am on that server sometimes).