import subprocess
import sys

def install_and_run():
    try:
        import rembg
    except ImportError:
        print("Installing rembg...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "rembg", "onnxruntime"])
        import rembg
    
    from rembg import remove
    from PIL import Image
    
    images = [
        r"C:\Users\ACER\.gemini\antigravity\brain\2c7742ae-026a-432b-86ed-42aaa9892996\btn_continue_wood_fantasy_1773197323626.png",
        r"C:\Users\ACER\.gemini\antigravity\brain\2c7742ae-026a-432b-86ed-42aaa9892996\dialogue_frame_wood_transparent_1773197274816.png"
    ]
    
    for path in images:
        print(f"Processing {path}...")
        input_image = Image.open(path)
        output_image = remove(input_image)
        new_path = path.replace(".png", "_nobg.png")
        output_image.save(new_path)
        print(f"Saved {new_path}")

if __name__ == "__main__":
    install_and_run()
