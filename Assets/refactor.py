import os
import re

directory = r"c:\Users\Admin\Documents\GitHub\CapstoneProject\Assets\Main Scripts\Player\Scripts\Main Scripts\New Character"

# 1. Update State.cs
state_file = os.path.join(directory, "State.cs")
with open(state_file, "r") as f:
    content = f.read()

content = re.sub(r'public InputAction .*?;\n', '', content)
content = re.sub(r'moveAction = .*?;\n', '', content)
content = re.sub(r'lookAction = .*?;\n', '', content)
content = re.sub(r'jumpAction = .*?;\n', '', content)
content = re.sub(r'crouchAction = .*?;\n', '', content)
content = re.sub(r'sprintAction = .*?;\n', '', content)
content = re.sub(r'dashAction = .*?;\n', '', content)
content = re.sub(r'toggleWeaponAction = .*?;\n', '', content)
content = re.sub(r'attackAction = .*?;\n', '', content)

helpers = """
    protected bool JumpTriggered => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_JUMP) && !character.previousInput.buttons.IsSet(NetworkInputData.BUTTON_JUMP);
    protected bool DashTriggered => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_DASH) && !character.previousInput.buttons.IsSet(NetworkInputData.BUTTON_DASH);
    protected bool ToggleWeaponTriggered => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_TOGGLE_WEAPON) && !character.previousInput.buttons.IsSet(NetworkInputData.BUTTON_TOGGLE_WEAPON);
    protected bool AttackTriggered => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_ATTACK) && !character.previousInput.buttons.IsSet(NetworkInputData.BUTTON_ATTACK);
    protected bool CrouchTriggered => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_CROUCH) && !character.previousInput.buttons.IsSet(NetworkInputData.BUTTON_CROUCH);
    
    protected bool SprintPressed => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_SPRINT);
    protected bool AttackPressed => character.currentInput.buttons.IsSet(NetworkInputData.BUTTON_ATTACK);
    protected Vector2 MoveInput => character.currentInput.movementInput;
"""
content = content.replace("protected Vector2 input;", "protected Vector2 input;\n" + helpers)

with open(state_file, "w") as f:
    f.write(content)

# 2. Iterate all states to replace InputActions and character.controller.Move
for filename in os.listdir(directory):
    if not filename.endswith("State.cs") and filename != "Character.cs":
        continue
    filepath = os.path.join(directory, filename)
    with open(filepath, "r") as f:
        text = f.read()
    
    # Replace inputs
    text = text.replace("moveAction.ReadValue<Vector2>()", "MoveInput")
    text = text.replace("jumpAction.triggered", "JumpTriggered")
    text = text.replace("dashAction.triggered", "DashTriggered")
    text = text.replace("toggleWeaponAction.triggered", "ToggleWeaponTriggered")
    text = text.replace("attackAction.triggered", "AttackTriggered")
    text = text.replace("crouchAction.triggered", "CrouchTriggered")
    text = text.replace("sprintAction.IsPressed()", "SprintPressed")
    text = text.replace("attackAction.IsPressed()", "AttackPressed")
    
    # Also replace character.jumpActionCache.triggered in Character.cs
    if filename == "Character.cs":
        text = text.replace("jumpActionCache != null && jumpActionCache.triggered", "(currentInput.buttons.IsSet(NetworkInputData.BUTTON_JUMP) && !previousInput.buttons.IsSet(NetworkInputData.BUTTON_JUMP))")
        text = text.replace("public NetworkInputData currentInput;", "public NetworkInputData previousInput;\n    public NetworkInputData currentInput;")
        text = text.replace("currentInput = input;", "previousInput = currentInput;\n            currentInput = input;")

    # Replace physics movement
    text = re.sub(r'character\.controller\.Move\((.*?)\);', r'character.CalculatedVelocity = \1 / Runner.DeltaTime;', text)
    # Actually wait, character.controller.Move(vel * Time.deltaTime) -> character.CalculatedVelocity = vel;
    # But usually it's passed as: character.controller.Move(currentVelocity * Time.deltaTime + gravityVelocity * Time.deltaTime);
    # To extract the velocity without DeltaTime:
    text = text.replace("character.controller.Move(currentVelocity * Time.deltaTime * (playerSpeed * speedMultiplier) + gravityVelocity * Time.deltaTime);", "character.CalculatedVelocity = currentVelocity * (playerSpeed * speedMultiplier) + gravityVelocity;")
    text = text.replace("character.controller.Move(currentVelocity * Time.deltaTime * sprintSpeed + gravityVelocity * Time.deltaTime);", "character.CalculatedVelocity = currentVelocity * sprintSpeed + gravityVelocity;")
    text = text.replace("character.controller.Move(currentVelocity * Time.deltaTime * crouchSpeed + gravityVelocity * Time.deltaTime);", "character.CalculatedVelocity = currentVelocity * crouchSpeed + gravityVelocity;")
    text = text.replace("character.controller.Move(currentVelocity * Time.deltaTime * dashSpeed);", "character.CalculatedVelocity = currentVelocity * dashSpeed;")
    text = text.replace("character.controller.Move(new Vector3(0f, gravityVelocity.y, 0f) * Time.deltaTime);", "character.CalculatedVelocity = new Vector3(0f, gravityVelocity.y, 0f);")
    text = text.replace("character.controller.Move(gravityVelocity * Time.deltaTime);", "character.CalculatedVelocity = gravityVelocity;")
    
    # Update Time.deltaTime -> Runner.DeltaTime inside states
    # Note: State classes don't have access to Runner directly unless we pass it. But we can use character.Runner.DeltaTime
    text = text.replace("Time.deltaTime", "character.Runner.DeltaTime")
    text = text.replace("Time.time", "character.Runner.SimulationTime")

    with open(filepath, "w") as f:
        f.write(text)

print("Refactoring done.")
