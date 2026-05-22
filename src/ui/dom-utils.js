/**
 * 指定タグの要素を生成し、任意で class と textContent を設定する。
 * @param {string} tag タグ名
 * @param {string} [className] 付与する class 名
 * @param {string} [text] 設定する textContent
 * @returns {HTMLElement} 生成した要素
 */
export function el(tag, className, text) {
  const node = document.createElement(tag);
  if (className) {
    node.className = className;
  }
  if (text !== undefined) {
    node.textContent = text;
  }
  return node;
}
