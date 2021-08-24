import os
import selenium
import time
from selenium import webdriver
from selenium.webdriver.common.keys import Keys


browser = webdriver.Chrome(os.getcwd() + "\chromedriver.exe")


browser.get("https://osu.ppy.sh/forum/ucp.php?mode=login")
elem = browser.find_element_by_name('username')
elem.clear()
elem.send_keys('XXX')
elem = browser.find_element_by_name('password')
elem.clear()
elem.send_keys('XXX')
elem.send_keys(Keys.RETURN)


mapsets = open("mapsets.txt", "r")
xPath = "/html/body/div[7]/div/div/div[2]/div[1]/div/div[2]/div[3]/a["
for id in mapsets:
    browser.get("https://osu.ppy.sh/beatmapsets/" + id)

    # elem = browser.find_element_by_class_name("js-beatmapset-download-link")
    try:
        elem = browser.find_element_by_xpath(xPath + "4]")
    except:
        elem = browser.find_element_by_xpath(xPath + "1]")
    else:
        elem = browser.find_element_by_xpath(xPath + "2]")
        
    elem.click()
    time.sleep(3)


mapsets.close()