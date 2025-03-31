import fontforge
# 打开已有字体文件
# font = fontforge.open("../Assets/MaoKenTangYuan.ttf")
font = fontforge.open("../Assets/MaoKenTangYuan.ttf")

# Set the weight to increase the stroke thickness
thickness = 20

# Iterate through all glyphs
for glyph in font.glyphs():
    if glyph.isWorthOutputting():
        # Convert to third-order splines to avoid invalid second-order splines
        glyph.correctDirection()
        glyph.simplify()
        # Remove overlaps only once, before modifying the weight
        glyph.removeOverlap()
        if glyph.isWorthOutputting():
            # Adjust the stroke weight (tune the value to your needs)
            glyph.changeWeight(thickness, "auto", 0, 0, "auto")
            # Optionally, remove overlaps again after changing the weight (if necessary)
            glyph.removeOverlap()
            glyph.correctDirection()
            glyph.simplify()

# Save the modified font as a new file
font.generate("MaoKenTangYuanBold.ttf")