import pyautogui
import time

interval = 2  # seconds
screenshot_key = 'numsub'

while True:
    pyautogui.press(screenshot_key)
    print("Screenshot taken")
    time.sleep(interval)
