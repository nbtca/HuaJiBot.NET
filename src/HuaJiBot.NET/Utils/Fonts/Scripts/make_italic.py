import fontforge
from math import tan
# 打开已有字体文件
font = fontforge.open("../Assets/MaoKenTangYuan.ttf")

# 对所有字符应用斜体变换
angle = 0.2  # 斜体角度（弧度）
for glyph in font.glyphs():
    if glyph.isWorthOutputting():
        glyph.transform((1, 0, tan(angle), 1, 0, 0))

# 保存为新的字体文件
font.generate("MaoKenTangYuanItalic.ttf")
