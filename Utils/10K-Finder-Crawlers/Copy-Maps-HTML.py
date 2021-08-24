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


beatmaps = []
url = "https://old.ppy.sh/p/beatmaplist?l=1&r=4&q=10k&g=0&la=0&ra=&s=4&o=1&m=3&page="
xPath = "/html/body/div[1]/div/div[1]/div[4]/div[2]/div[5]"
for i in range(1, 20 + 1):
    browser.get(url + str(i))
    elem = browser.find_element_by_xpath(xPath)
    beatmaps.append(elem.get_attribute("innerHTML"))
    time.sleep(2)

f = open("result.txt", "w", encoding="utf-16")
f.write("".join(beatmaps))
f.close()