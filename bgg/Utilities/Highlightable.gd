# Handles highlighting of Area2Ds
#
# Expects Parent to have handle_input()
# Expects children:
# 	Shape - CollisionShape2D

extends Area2D

signal state_changed

onready var par = get_parent()
onready var gm = get_node('/root/GameManager')
var is_highlighted = false

func _ready():
	connect('mouse_entered', self, '_on_mouse_enter')
	connect('mouse_exited', self, '_on_mouse_exit')

func _exit_tree():
	if is_highlighted:
		mark_as_unhighlighted()

func _on_mouse_enter():
	mark_as_highlighted()

func _on_mouse_exit():
	mark_as_unhighlighted()

# For when markers are manually moved
func mark_as_highlighted():
	gm.req_highlight(self)

# For when markers are manually moved
func mark_as_unhighlighted():
	gm.req_unhighlight(self)

# Highlightable Interface
# >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
func handle_input(ev):
	par.handle_input(ev)

func highlight():
	is_highlighted = true
	emit_signal("state_changed")

func unhighlight():
	is_highlighted = false
	emit_signal("state_changed")
# <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<