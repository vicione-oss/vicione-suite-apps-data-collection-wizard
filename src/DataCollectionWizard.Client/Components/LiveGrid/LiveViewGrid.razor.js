export class LiveViewGrid {

    /**
     * Scrolls the viewport of the provided element to the top
     * @param {HTMLElement} element
     */
    static resetScrollPosition = function(element) {
        element.scrollTop = 0;
    };
}
