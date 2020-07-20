# COUNTERS
Windows performance counters doodle

Ideas for future releases
* add new type of icon - vertical bar, horizontal bar (both static, just another shape)
* add new option for rectangular leds - variable size to show counter value
* OR even more - each led to have variable size, depending on shape - length for bars, radius for circle
* variable circle could be filled or not
* led brightness according to value or simple 0/1 blink
* blink on/off for variable rectangle
* make it possible to add/remove multiple counters, each with it's icon
* hence, change of program design and logic - add new level of astraction - "counter" object
* each counter object would have it's own menu (just like current tray menu), with "remove option" plus LED object as usual
* main program has List<Counters>, earch time counter ias added, it's menu property is hooked to tray icon's menu
* all (or most) event handlers are already written - counter clicks, color/shape clicks
* main menu on startup would have exit and add counter options
* Challenge - is it possible to save counter list in INI file? No problems with saving parameters of subsequent counters. For example each of them would be called counter01param etc. And then we'd have to save new parameter to INI saying how many counters are saved. Then startup loop would read this parm and try to read counter[ix]params to recreated saved counters, adding them to the tray menu and to tray.
* This would be useful to have f.ex. read and write disk leds
* Or one led per each cpu core
