# Project-Laser
 Laser created from LineRenderer 


### Options
Allow you to create light rays reflected on surface, creating instance of GameObject egz. particle on point of contact  
You can control lenght of the ray, limit reflections.
Ray can absorb color of surface using one of three mixing ways:
* Copy color
* Mix Color 
* No change 

Object spawning is using simple pooling system, rotation of spawned object can be set as:
* Inherit from hiten object
* Normal of ray and hiten object

Laser contain and update(if selected) list of all hiten object in order. 
