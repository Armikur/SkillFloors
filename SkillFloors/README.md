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
* Sound and Visuals when a skill floor levels up

## Changelog
| Version | Notes
| ------- | ----- |
| 1.1.3 | <ul><li>fixed an issue where new characters could inherit other characters' SkillFloor levels</li><li>SkillFloors cannot be higher than their associated skill and won't gain XP if the floor and skill are the same level.</li></ul>
| 1.1.2 | <ul><li>added secondary progress bar to Skills panel</li><li>added configurable progress rate</li><li>can now toggle extra debug messages in BepInEx window</li><li>added BepInEx Configuration Manager support for config settings</li><li>added server sync (hopefully working!)</li></ul>
| 1.0.2 | <ul><li>other last minute tweaks and readme corrections</li></ul>
| 1.0.1 | <ul><li>fewer debug messages</li></ul>
| 1.0.0 | <ul><li>first working release!</li></ul>

## Known issues
* The skill floor progress bar might disappear sometimes. This does not affect the actual skill floor's level progress, only the bar's display on the skills list.

## Other / Support
* You can find the github at: https://github.com/Armikur/SkillFloors
* If you have any issues, please open an issue on the github page or @Armikur on the [Valheim Modding Discord](https://discord.com/invite/GUEBuCuAMz). (I'm not a member of that team, but I am on that server sometimes).