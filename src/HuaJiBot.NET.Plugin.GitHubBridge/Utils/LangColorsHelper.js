//从搜索栏获取所有语言及其颜色
//https://github.com/search?q=
//网页控制台运行

await(async () => {
    // Convert the RGB color to hexadecimal
    function rgbToHex(rgb) {
        // Check if the string starts with 'rgb'
        if (rgb.indexOf("rgb") === -1) {
            return rgb; // Return the value as it is if it's not an RGB color
        }
        // Extract the individual RGB values
        var [r, g, b] = rgb.match(/\d+/g);
        // Convert each RGB value to hexadecimal
        r = parseInt(r).toString(16).padStart(2, '0');
        g = parseInt(g).toString(16).padStart(2, '0');
        b = parseInt(b).toString(16).padStart(2, '0');
        // Concatenate the hexadecimal values with '#' to form the hexadecimal color code
        return {
            hex: '#' + r + g + b,
            r: r,
            g: g,
            b: b
        }
    }
    function Input(textBox, text) {
        // set the text value
        textBox.value = text;
        // raise the input event
        var eventInput = new InputEvent("input", { inputType: "insertText", data: text, bubbles: true });
        textBox.dispatchEvent(eventInput);
    }
    const dict = {};
    {//get
        const textBox = document.querySelector("#query-builder-test");
        const textToType = "language:";
        async function testItem(lang) {
            Input(textBox, textToType + lang);
            await new Promise(r => setTimeout(r, 800));
            for (const item of document.getElementsByClassName('QueryBuilder-ListItem')) {
                const lang = item.children[1].textContent.trim();
                if (key.startsWith("language:")) {
                    continue;
                }
                const color = item.children[0].children[0].style.backgroundColor;
                dict[lang] = color;
            }
        }
        //from aa to zz
        for (let i = 97; i < 123; i++) {
            await testItem(String.fromCharCode(i));
            for (let j = 97; j < 123; j++) {
                await testItem(String.fromCharCode(i) + String.fromCharCode(j));
            }
            await testItem(String.fromCharCode(i));
        }
    }
    {//print
        let result = ""
        for (const [key, value] of Object.entries(dict)) { 
            const {
                r,
                g,
                b
            } = rgbToHex(value);
            result += `["${key}"]= (0x${r}, 0x${g}, 0x${b}),\n`;
        }
        console.log(result);
    }
})()