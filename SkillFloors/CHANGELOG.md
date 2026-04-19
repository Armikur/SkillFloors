# Changelog

## 1.1.4
* fixed a save data migration bug in 1.1.3. Please skip 1.1.3!
* more verbose debug logging if you turn debug on in SkillFloors config.
* fixed documentation typos

## 1.1.3 (2026-04-18)
* fixed an issue where new characters could inherit other characters' SkillFloor levels
* prevents skill floors from being higher their associated skill and won't gain XP if the floor and skill are the same level.
* moving from JSON to ZPackage saves (deprecated JsonDotNET dependency). Will be removed in 1.2 update.

## 1.1.2
* added text and progress bar to Skills panel
* Now configurable (works with BepInEx Configuration Manager):
	* Floor XP gain rate (default 15% of normal skill rate)
	* Frequent BepInEx window messages are toggleable
* ServerSync for floor XP gain rate

## 1.0.2
* other last minute tweaks and readme corrections

## 1.0.1
* fewer debug messages

## 1.0.0
* first working release!