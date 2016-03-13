Xmr PDF Editor
==================

Gtk# based PDF Editor for the Xmr project

## TODO:
*  Add start-up dialog on first run, and save default directory in a properties file
*  Make PDFs load asynchronously
*  Fix better view of PDFs
*  Holding shift during selection should connect the current selection
   with the previous one. If previous image was 57, next one should be
   57 also, not 58 if shift is held.
*  Have a list of all rectangles, so you can select one and delete it,
   in case it is 0 pixels tall.
*  Fix output directory somehow
*  Fix long images, eda040 problem questions were merged improperly.
*  Save state to JSON or something so we can reload it when we fuckup.
   Ensure that each input results into a new version of the JSON file, 
   so that we can step between the changes. OVERKILL, but fun?
*  Sort pages by lexicographical ordering, 10 should come after 09 or 9 for example.
