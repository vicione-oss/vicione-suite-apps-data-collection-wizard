export class DataCollectionWizardSidebar {

    /**
     * Attaches a smooth, pointer-capture based resize to the sidebar's right edge. During the drag the sidebar's
     * --width CSS variable is driven directly (no per-move round-trip to .NET, so no stutter), and because the pointer
     * is captured the drag never loses the cursor. The final width is reported back once, on release.
     * @param {HTMLElement} handle - the invisible grab strip at the sidebar's right edge
     * @param {HTMLElement} host - the wrapper element that contains the house .sidebar element
     * @param {any} dotnetRef - .NET reference used to persist the final width
     * @param {number} min - minimum sidebar width in px
     * @param {number} max - maximum sidebar width in px
     */
    static attachResize = function (handle, host, dotnetRef, min, max) {
        const sidebar = host ? host.querySelector('.sidebar') : null;
        if (!handle || !sidebar) {
            return;
        }

        let dragging = false;
        let startX = 0;
        let startWidth = 0;
        let pointerId = 0;

        const onMove = function (e) {
            if (!dragging) {
                return;
            }
            let width = startWidth + (e.clientX - startX);
            width = Math.max(min, Math.min(max, width));
            sidebar.style.setProperty('--width', width + 'px');
        };

        const onUp = function () {
            if (!dragging) {
                return;
            }
            dragging = false;
            try {
                handle.releasePointerCapture(pointerId);
            } catch (ignored) {
                // Pointer capture may already be released; ignore.
            }
            handle.removeEventListener('pointermove', onMove);
            handle.removeEventListener('pointerup', onUp);
            sidebar.style.userSelect = '';

            const finalWidth = Math.round(sidebar.getBoundingClientRect().width);
            dotnetRef.invokeMethodAsync('SetFluidWidth', finalWidth);
        };

        handle.addEventListener('pointerdown', function (e) {
            if (e.button !== 0) {
                return;
            }
            dragging = true;
            startX = e.clientX;
            startWidth = sidebar.getBoundingClientRect().width;
            pointerId = e.pointerId;
            handle.setPointerCapture(pointerId);
            handle.addEventListener('pointermove', onMove);
            handle.addEventListener('pointerup', onUp);
            sidebar.style.userSelect = 'none';
            e.preventDefault();
        });
    };
}
