import fnmatch
import cv2
import numpy
import os

# resize png files
def resize_img (png_file, png_name):
    
    print(png_file)
    
    img = cv2.imread('{}'.format(png_file), -1)
    res_img = cv2.resize(img, dsize=(24, 24), interpolation=cv2.INTER_AREA)

    print(img.item)

    print('original Size:', img.shape)
    print('rezized Size:', res_img.shape)

    status = cv2.imwrite("24x24_" + png_name, res_img)

# main
# folder path
folder_path = os.path.dirname(os.path.realpath(__file__))

for img in os.listdir(folder_path):
    if fnmatch.fnmatch(img, '*.png'):
        image = folder_path + '/' + img
        resize_img(image, img)