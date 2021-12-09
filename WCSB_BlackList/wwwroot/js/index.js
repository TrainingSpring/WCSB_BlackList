/**
 * @desc 获取元素的高度
 * @element 元素
 */
function getHeight(el,box){
    let base = 46;
    if (box.querySelector(".t-title").style.display === "none") {
        base = 20;
    }
    box.style.height = (el.clientHeight + base) + "px"
    return el.clientHeight;
}