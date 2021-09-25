import os
import time
from selenium import webdriver
from selenium.webdriver.common.keys import Keys

options = webdriver.ChromeOptions()
options.headless = True
options.add_argument("--log-level=3")
browser = webdriver.Chrome(options=options, executable_path=os.getcwd() + "\chromedriver.exe")
browser.set_window_size(1920, 1080)

browser.get("https://www.facebook.com/")
elem = browser.find_element_by_xpath("/html/body/div[3]/div[2]/div/div/div/div/div[3]/button[2]")
elem.click()
elem = browser.find_element_by_id("email")
elem.send_keys('XXX')
elem = browser.find_element_by_name('pass')
elem.send_keys('XXX')
elem.send_keys(Keys.RETURN)
time.sleep(5)

ids = open("ids.txt", "r").readlines()
idsLen = len(ids)
i = 1
for id in ids:
    id = id.strip("\n")
    print(f"{i}/{idsLen}: {id}")
    
    browser.get("https://www.facebook.com/photo.php?fbid=" + id)
    time.sleep(1)

    browser.find_element_by_tag_name("body").screenshot("img/" + id + ".png")

    i += 1

browser.quit()
print("Done")