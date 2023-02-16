# Procedural_Labyrinth_Generation 

## The Main source code is not finished, currently only provide Unity sample

 A Script Work on Unity
 
 See more detail in the Unity branch
 
 Concept of Implement
 - Space_Set = Generate_Space(number, feature, location) 
 - Physical_engine(Space_Set) // to separate the conponents in the Space_Set to avoid overlapping
 - Draw_Path(Space_Set)  // selective connect each conponents in the Space_Set, default is a variant of MST
