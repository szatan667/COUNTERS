# COUNTERS
Windows performance counters doodle

Ideas for future releases
* TODO add new option for rectangular leds - variable size to show counter value
* TODO even more - each led to have variable size, depending on shape - length for bars, radius for circle
* TODO variable circle could be filled or not
* TODO led brightness according to value or simple 0/1 blink
* TODO blink on/off for all shapes
* DONE! add new type of icon - vertical bar, horizontal bar (both static, just another shape)
* DONE! make it possible to add/remove multiple counters, each with it's icon
* DONE! hence, change of program design and logic - add new level of astraction - "counter" object
* DONE! each counter object would have it's own menu (just like current tray menu), with "remove option" plus LED object as usual
* DONE! main program has List<Counters>, earch time counter ias added, it's menu property is hooked to tray icon's menu
* DONE! all (or most) event handlers are already written - counter clicks, color/shape clicks
* DONE! main menu on startup would have exit and add counter options
* DONE! Challenge - is it possible to save counter list in INI file? No problems with saving parameters of subsequent counters. For example each of them would be called counter01param etc. And then we'd have to save new parameter to INI saying how many counters are saved. Then startup loop would read this parm and try to read counter[ix]params to recreated saved counters, adding them to the tray menu and to tray.
* DONE! This would be useful to have f.ex. read and write disk leds
* DONE! Or one led per each cpu core
