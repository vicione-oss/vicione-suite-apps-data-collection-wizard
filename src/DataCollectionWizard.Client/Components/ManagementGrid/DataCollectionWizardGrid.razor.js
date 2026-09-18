export class DataCollectionWizardGrid {

    /**
     * Scrolls the viewport of the provided element to the top
     * @param {HTMLElement} element
     */
    static resetScrollPosition = function(element) {
        element.scrollTop = 0;
    };

    /**
     * Lets the user drag the floating bulk bar out of the way via a grip. Pointer-capture based (never loses the
     * cursor) and pins the bar to an absolute position, so it stays where it is dropped.
     * @param {HTMLElement} handle - the grip element
     * @param {HTMLElement} bar - the bulk bar to move
     */
    static makeDraggable = function (handle, bar) {
        if (!handle || !bar) {
            return;
        }

        let dragging = false;
        let pointerId = 0;
        let startX = 0;
        let startY = 0;
        let startLeft = 0;
        let startTop = 0;

        const onMove = function (e) {
            if (!dragging) {
                return;
            }
            bar.style.left = (startLeft + (e.clientX - startX)) + "px";
            bar.style.top = (startTop + (e.clientY - startY)) + "px";
        };

        const onUp = function () {
            if (!dragging) {
                return;
            }
            dragging = false;
            try {
                handle.releasePointerCapture(pointerId);
            } catch (ignored) {
                // capture may already be released
            }
            handle.removeEventListener("pointermove", onMove);
            handle.removeEventListener("pointerup", onUp);
            // Restore the CSS transition (kept for the fade); the transform stays pinned so nothing animates.
            bar.style.transition = "";
        };

        handle.addEventListener("pointerdown", function (e) {
            if (e.button !== 0) {
                return;
            }
            const rect = bar.getBoundingClientRect();
            dragging = true;
            pointerId = e.pointerId;
            startX = e.clientX;
            startY = e.clientY;
            startLeft = rect.left;
            startTop = rect.top;
            // Disable the transition first so switching from the centered transform to an absolute position does not
            // animate ("jump") on the first drag.
            bar.style.transition = "none";
            // Pin to an absolute position so the drag is 1:1 (drop the translateX(-50%) centering and bottom anchor).
            bar.style.left = rect.left + "px";
            bar.style.top = rect.top + "px";
            bar.style.right = "auto";
            bar.style.bottom = "auto";
            bar.style.transform = "none";
            handle.setPointerCapture(pointerId);
            handle.addEventListener("pointermove", onMove);
            handle.addEventListener("pointerup", onUp);
            e.preventDefault();
        });
    };
}
